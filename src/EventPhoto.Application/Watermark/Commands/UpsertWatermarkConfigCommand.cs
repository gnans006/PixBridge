using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Watermark.Commands;

/// <summary>
/// Creates or replaces the watermark configuration for an event.
/// </summary>
public sealed record UpsertWatermarkConfigCommand(
    Guid EventId,
    bool Enabled,
    string Mode,
    string Style,
    float Opacity,
    string Scale,
    string? CustomText,
    string? Template,
    string? LogoPath,
    bool IncludeStudioName,
    bool IncludeEventName,
    bool IncludeDownloadDate,
    bool ApplyOnDownload,
    string TextColor,
    string? FontName,
    float BackgroundOpacity,
    bool ApplyOnPreview)
    : IRequest<Result<WatermarkConfigResponse>>;

/// <summary>Handles <see cref="UpsertWatermarkConfigCommand"/>.</summary>
public sealed class UpsertWatermarkConfigCommandHandler(
    IWatermarkConfigurationRepository watermarkRepository,
    IEventRepository eventRepository,
    IWatermarkCacheService watermarkCache,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertWatermarkConfigCommand, Result<WatermarkConfigResponse>>
{
    /// <inheritdoc />
    public async Task<Result<WatermarkConfigResponse>> Handle(
        UpsertWatermarkConfigCommand request,
        CancellationToken cancellationToken)
    {
        var eventExists = await eventRepository.ExistsAsync(request.EventId, cancellationToken);
        if (!eventExists)
        {
            return Result.Failure<WatermarkConfigResponse>($"Event '{request.EventId}' was not found.");
        }

        if (!Enum.TryParse<WatermarkMode>(request.Mode, ignoreCase: true, out var mode))
        {
            return Result.Failure<WatermarkConfigResponse>($"Invalid watermark mode: '{request.Mode}'.");
        }

        if (!Enum.TryParse<WatermarkStyle>(request.Style, ignoreCase: true, out var style))
        {
            return Result.Failure<WatermarkConfigResponse>($"Invalid watermark style: '{request.Style}'.");
        }

        if (!Enum.TryParse<WatermarkScale>(request.Scale, ignoreCase: true, out var scale))
        {
            return Result.Failure<WatermarkConfigResponse>($"Invalid watermark scale: '{request.Scale}'.");
        }

        var config = await watermarkRepository.GetByEventIdAsync(request.EventId, cancellationToken);

        if (config is null)
        {
            config = WatermarkConfiguration.CreateForEvent(request.EventId);
            config.Update(
                request.Enabled, mode, style, request.Opacity, scale,
                request.CustomText, request.Template, request.LogoPath,
                request.IncludeStudioName, request.IncludeEventName,
                request.IncludeDownloadDate, request.ApplyOnDownload,
                request.TextColor, request.FontName,
                request.BackgroundOpacity, request.ApplyOnPreview);

            await watermarkRepository.AddAsync(config, cancellationToken);
        }
        else
        {
            config.Update(
                request.Enabled, mode, style, request.Opacity, scale,
                request.CustomText, request.Template, request.LogoPath,
                request.IncludeStudioName, request.IncludeEventName,
                request.IncludeDownloadDate, request.ApplyOnDownload,
                request.TextColor, request.FontName,
                request.BackgroundOpacity, request.ApplyOnPreview);

            await watermarkRepository.UpdateAsync(config, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate all cached watermarked files for this event — the new config
        // hash will differ, so guests get freshly watermarked files on next download.
        watermarkCache.InvalidateEvent(request.EventId);

        return Result.Success(MapToResponse(config));
    }

    private static WatermarkConfigResponse MapToResponse(WatermarkConfiguration c) =>
        new(
            c.Id, c.EventId, c.Enabled,
            c.Mode.ToString(), c.Style.ToString(),
            c.Opacity, c.Scale.ToString(),
            c.CustomText, c.Template, c.LogoPath,
            c.IncludeStudioName, c.IncludeEventName,
            c.IncludeDownloadDate, c.ApplyOnDownload,
            c.TextColor, c.FontName,
            c.BackgroundOpacity, c.ApplyOnPreview,
            c.CreatedAt, c.UpdatedAt);
}
