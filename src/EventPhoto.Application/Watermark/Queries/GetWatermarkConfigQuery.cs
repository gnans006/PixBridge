using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Watermark.Queries;

/// <summary>
/// Returns the watermark configuration for a given event.
/// When no configuration exists, a default disabled configuration is returned
/// without persisting it.
/// </summary>
/// <param name="EventId">The event identifier.</param>
public sealed record GetWatermarkConfigQuery(Guid EventId)
    : IRequest<Result<WatermarkConfigResponse>>;

/// <summary>Handles <see cref="GetWatermarkConfigQuery"/>.</summary>
public sealed class GetWatermarkConfigQueryHandler(
    IWatermarkConfigurationRepository watermarkRepository,
    IEventRepository eventRepository)
    : IRequestHandler<GetWatermarkConfigQuery, Result<WatermarkConfigResponse>>
{
    /// <inheritdoc />
    public async Task<Result<WatermarkConfigResponse>> Handle(
        GetWatermarkConfigQuery request,
        CancellationToken cancellationToken)
    {
        var eventExists = await eventRepository.ExistsAsync(request.EventId, cancellationToken);
        if (!eventExists)
        {
            return Result.Failure<WatermarkConfigResponse>($"Event '{request.EventId}' was not found.");
        }

        var config = await watermarkRepository.GetByEventIdAsync(request.EventId, cancellationToken);

        // Return a transient default when no row exists yet.
        config ??= WatermarkConfiguration.CreateForEvent(request.EventId);

        return Result.Success(MapToResponse(config));
    }

    private static WatermarkConfigResponse MapToResponse(WatermarkConfiguration c) =>
        new(
            c.Id,
            c.EventId,
            c.Enabled,
            c.Mode.ToString(),
            c.Style.ToString(),
            c.Opacity,
            c.Scale.ToString(),
            c.CustomText,
            c.Template,
            c.LogoPath,
            c.IncludeStudioName,
            c.IncludeEventName,
            c.IncludeDownloadDate,
            c.ApplyOnDownload,
            c.TextColor,
            c.FontName,
            c.BackgroundOpacity,
            c.ApplyOnPreview,
            c.CreatedAt,
            c.UpdatedAt);
}
