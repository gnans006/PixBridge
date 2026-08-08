using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Photos.Queries;

/// <summary>
/// Query that reads the raw bytes of a photo's original file, applies any active watermark,
/// records a download event, and returns the result.
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
/// </summary>
public sealed class DownloadPhotoQueryHandler(
    IPhotoRepository photoRepository,
    IDownloadLogRepository downloadLogRepository,
    IFileService fileService,
    IUnitOfWork unitOfWork,
    IWatermarkConfigurationRepository watermarkRepository,
    IWatermarkService watermarkService,
    IEventRepository eventRepository,
    IApplicationSettingsRepository applicationSettingsRepository)
    : IRequestHandler<DownloadPhotoQuery, Result<DownloadResult>>
{
    /// <inheritdoc />
    public async Task<Result<DownloadResult>> Handle(
        DownloadPhotoQuery request,
        CancellationToken cancellationToken)
    {
        var photo = await photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null)
        {
            return Result.Failure<DownloadResult>($"Photo '{request.PhotoId}' was not found.");
        }

        if (!fileService.FileExists(photo.OriginalPath))
        {
            return Result.Failure<DownloadResult>("The photo file could not be found on disk.");
        }

        var bytes = await fileService.ReadAllBytesAsync(photo.OriginalPath, cancellationToken);

        // ── Watermarking ─────────────────────────────────────────────────────
        try
        {
            var watermarkConfig = await watermarkRepository.GetByEventIdAsync(photo.EventId, cancellationToken);
            if (watermarkConfig is { Enabled: true, ApplyOnDownload: true }
                && watermarkConfig.Mode != WatermarkMode.Disabled)
            {
                var eventEntity = await eventRepository.GetByIdAsync(photo.EventId, cancellationToken);
                var appSettings = await applicationSettingsRepository.GetOrCreateDefaultAsync(cancellationToken);

                var context = new WatermarkContext(
                    StudioName: appSettings.StudioName,
                    EventName: eventEntity?.Name ?? string.Empty,
                    EventDate: eventEntity?.EventDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    DownloadDate: DateTimeOffset.UtcNow,
                    PhotoName: photo.FileName,
                    SessionId: request.SessionId);

                bytes = await watermarkService.ApplyWatermarkAsync(bytes, watermarkConfig, context, cancellationToken);
            }
        }
        catch (Exception)
        {
            // Watermark lookup failed (e.g. table not yet migrated). Continue with original bytes.
        }

        // ── Record download ──────────────────────────────────────────────────
        photo.RecordDownload();
        await photoRepository.UpdateAsync(photo, cancellationToken);

        var log = Domain.Entities.DownloadLog.Create(
            photo.Id,
            photo.EventId,
            request.IpAddress,
            request.UserAgent);

        await downloadLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new DownloadResult(bytes, photo.MimeType, photo.FileName));
    }
}
