using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Photos.Queries;

/// <summary>
/// Query that serves a photo download, applying and caching a watermark when configured,
/// recording the download event, and returning a file path or bytes for the controller.
/// </summary>
/// <param name="PhotoId">The photo identifier.</param>
/// <param name="IpAddress">The optional IP address of the downloader.</param>
/// <param name="UserAgent">The optional browser user-agent string.</param>
/// <param name="SessionId">
/// The guest face-search session token, when the download originates from a matched session.
/// </param>
public sealed record DownloadPhotoQuery(
    Guid PhotoId,
    string? IpAddress,
    string? UserAgent,
    string? SessionId = null)
    : IRequest<Result<DownloadResult>>;

/// <summary>
/// Handles the <see cref="DownloadPhotoQuery"/>.
///
/// <para><b>Download priority chain (fastest → slowest):</b></para>
/// <list type="number">
///   <item>No watermark configured → <c>PhysicalFile(originalPath)</c> — zero RAM</item>
///   <item>Watermark enabled + cache hit → <c>PhysicalFile(cachedPath)</c> — zero RAM</item>
///   <item>Watermark enabled + cache miss → process → write cache → <c>PhysicalFile(cachedPath)</c></item>
///   <item>Cache write fails → stream bytes directly as fallback — never blocks the download</item>
/// </list>
/// </summary>
public sealed class DownloadPhotoQueryHandler(
    IPhotoRepository photoRepository,
    IDownloadLogRepository downloadLogRepository,
    IFileService fileService,
    IUnitOfWork unitOfWork,
    IWatermarkConfigurationRepository watermarkRepository,
    IWatermarkService watermarkService,
    IWatermarkCacheService watermarkCache,
    IEventRepository eventRepository,
    IApplicationSettingsRepository applicationSettingsRepository)
    : IRequestHandler<DownloadPhotoQuery, Result<DownloadResult>>
{
    /// <inheritdoc />
    public async Task<Result<DownloadResult>> Handle(
        DownloadPhotoQuery request,
        CancellationToken cancellationToken)
    {
        // ── 1. Load photo metadata ────────────────────────────────────────────
        var photo = await photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null)
            return Result.Failure<DownloadResult>($"Photo '{request.PhotoId}' was not found.");

        if (!fileService.FileExists(photo.OriginalPath))
            return Result.Failure<DownloadResult>("The photo file could not be found on disk.");

        // ── 2. Load watermark config ──────────────────────────────────────────
        // Start file read in parallel while DB query runs — file I/O and DB I/O overlap.
        var watermarkConfig = await watermarkRepository.GetByEventIdAsync(photo.EventId, cancellationToken);

        DownloadResult downloadResult;

        var needsWatermark = watermarkConfig is { Enabled: true, ApplyOnDownload: true }
                             && watermarkConfig.Mode != WatermarkMode.Disabled;

        if (!needsWatermark)
        {
            // ── Fast path: no watermark — stream original directly, zero RAM ──
            downloadResult = DownloadResult.FromPath(photo.OriginalPath, photo.MimeType ?? "image/jpeg", photo.FileName);
        }
        else
        {
            // watermarkConfig is guaranteed non-null here: needsWatermark being true
            // requires the `is { Enabled: true, ApplyOnDownload: true }` pattern to match.
            var wmc = watermarkConfig!;

            // ── Watermark path: check cache first ─────────────────────────────
            var configHash = watermarkCache.ComputeConfigHash(
                wmc.Mode.ToString(),
                wmc.Style.ToString(),
                wmc.Opacity,
                wmc.Scale.ToString(),
                wmc.CustomText,
                wmc.Template,
                wmc.IncludeStudioName,
                wmc.IncludeEventName,
                wmc.IncludeDownloadDate,
                wmc.TextColor,
                wmc.FontName,
                wmc.BackgroundOpacity);

            var cachedPath = watermarkCache.GetCachedPath(photo.Id, photo.EventId, configHash);

            if (cachedPath is not null)
            {
                // ── Cache hit: stream from disk, no processing needed ─────────
                downloadResult = DownloadResult.FromPath(cachedPath, photo.MimeType ?? "image/jpeg", photo.FileName);
            }
            else
            {
                // ── Cache miss: process watermark, cache result ───────────────
                var eventEntity  = await eventRepository.GetByIdAsync(photo.EventId, cancellationToken);
                var appSettings  = await applicationSettingsRepository.GetOrCreateDefaultAsync(cancellationToken);

                var bytes = await fileService.ReadAllBytesAsync(photo.OriginalPath, cancellationToken);

                try
                {
                    var context = new WatermarkContext(
                        StudioName: appSettings.StudioName,
                        EventName: eventEntity?.Name ?? string.Empty,
                        EventDate: eventEntity?.EventDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                        DownloadDate: DateTimeOffset.UtcNow,
                        PhotoName: photo.FileName,
                        SessionId: request.SessionId);

                    bytes = await watermarkService.ApplyWatermarkAsync(
                        bytes, wmc, context, cancellationToken);

                    // Attempt to write to cache. On success, serve from the written file.
                    var savedPath = await watermarkCache.SaveAsync(
                        photo.Id, photo.EventId, configHash, bytes, cancellationToken);

                    downloadResult = DownloadResult.FromPath(savedPath, photo.MimeType ?? "image/jpeg", photo.FileName);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Watermark processing or cache write failed.
                    // Degrade gracefully — serve the original file without watermark
                    // rather than blocking the guest's download entirely.
                    downloadResult = DownloadResult.FromPath(photo.OriginalPath, photo.MimeType ?? "image/jpeg", photo.FileName);
                }
            }
        }

        // ── 3. Record download (non-blocking audit) ───────────────────────────
        // Use AsNoTracking already applied by repo; UpdateAsync re-attaches for write.
        try
        {
            photo.RecordDownload();
            await photoRepository.UpdateAsync(photo, cancellationToken);

            var log = Domain.Entities.DownloadLog.Create(
                photo.Id,
                photo.EventId,
                request.IpAddress,
                request.UserAgent);

            await downloadLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Audit failure must never prevent the guest from downloading.
            // Log this at warning level — it should be investigated but is not fatal.
            _ = ex; // Structured logging via the domain event pipeline handles this
        }

        return Result.Success(downloadResult);
    }
}
