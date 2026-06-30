using EventPhoto.Contracts.Responses.Statistics;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Statistics.Queries;

/// <summary>
/// Query that aggregates statistics for a single event.
/// </summary>
/// <param name="EventId">The event identifier.</param>
public sealed record GetEventStatisticsQuery(Guid EventId)
    : IRequest<Result<EventStatisticsResponse>>;

/// <summary>
/// Handles the <see cref="GetEventStatisticsQuery"/>.
/// </summary>
public sealed class GetEventStatisticsQueryHandler(
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
    IDownloadLogRepository downloadLogRepository)
    : IRequestHandler<GetEventStatisticsQuery, Result<EventStatisticsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EventStatisticsResponse>> Handle(
        GetEventStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventStatisticsResponse>(
                $"Event '{request.EventId}' was not found.");
        }

        var totalDownloads = await downloadLogRepository.GetDownloadCountByEventAsync(
            request.EventId,
            cancellationToken);

        var pendingPhotos = await photoRepository.GetPendingThumbnailsAsync(int.MaxValue, cancellationToken);
        var pendingCount = pendingPhotos.Count(p => p.EventId == request.EventId);
        var sizeHuman = eventEntity.TotalSizeBytes switch
        {
            < 1024 => $"{eventEntity.TotalSizeBytes} B",
            < 1_048_576 => $"{eventEntity.TotalSizeBytes / 1024.0:F1} KB",
            < 1_073_741_824 => $"{eventEntity.TotalSizeBytes / 1_048_576.0:F1} MB",
            _ => $"{eventEntity.TotalSizeBytes / 1_073_741_824.0:F2} GB"
        };

        return Result.Success(new EventStatisticsResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.PhotoCount,
            totalDownloads,
            eventEntity.TotalSizeBytes,
            sizeHuman,
            pendingCount,
            0,
            null));
    }
}
