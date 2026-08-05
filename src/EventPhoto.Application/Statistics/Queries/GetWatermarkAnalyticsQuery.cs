using EventPhoto.Contracts.Responses.Statistics;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Statistics.Queries;

/// <summary>Returns watermark analytics across all events.</summary>
public sealed record GetWatermarkAnalyticsQuery : IRequest<Result<WatermarkAnalyticsResponse>>;

/// <summary>Handles <see cref="GetWatermarkAnalyticsQuery"/>.</summary>
public sealed class GetWatermarkAnalyticsQueryHandler(
    IEventRepository eventRepository,
    IDownloadLogRepository downloadLogRepository,
    IWatermarkConfigurationRepository watermarkRepository)
    : IRequestHandler<GetWatermarkAnalyticsQuery, Result<WatermarkAnalyticsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<WatermarkAnalyticsResponse>> Handle(
        GetWatermarkAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var allEvents = await eventRepository.GetAllAsync(cancellationToken);
        var totalDownloads = await downloadLogRepository.GetTotalCountAsync(cancellationToken);

        var eventsWithWatermark = 0;
        var activeWatermarkEvents = 0;
        var protectedDownloads = 0;

        foreach (var evt in allEvents)
        {
            var wm = await watermarkRepository.GetByEventIdAsync(evt.Id, cancellationToken);
            if (wm is not { Enabled: true }) continue;

            eventsWithWatermark++;
            if (evt.IsActive) activeWatermarkEvents++;
            protectedDownloads += await downloadLogRepository.GetDownloadCountByEventAsync(
                evt.Id, cancellationToken);
        }

        var coverage = allEvents.Count > 0
            ? Math.Round((double)eventsWithWatermark / allEvents.Count * 100, 1)
            : 0.0;

        return Result.Success(new WatermarkAnalyticsResponse(
            eventsWithWatermark,
            allEvents.Count,
            totalDownloads,
            protectedDownloads,
            coverage,
            activeWatermarkEvents));
    }
}
