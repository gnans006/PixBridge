using EventPhoto.Application.Common.Interfaces;
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
    IDownloadLogRepository downloadLogRepository,
    IFileStorageService fileStorageService)
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

        var liveSizeBytes = fileStorageService.GetFolderSize(eventEntity.WatchFolder);
        var sizeHuman = liveSizeBytes switch
        {
            < 1024 => $"{liveSizeBytes} B",
            < 1_048_576 => $"{liveSizeBytes / 1024.0:F1} KB",
            < 1_073_741_824 => $"{liveSizeBytes / 1_048_576.0:F1} MB",
            _ => $"{liveSizeBytes / 1_073_741_824.0:F2} GB"
        };

        return Result.Success(new EventStatisticsResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.PhotoCount,
            totalDownloads,
            liveSizeBytes,
            sizeHuman));
    }
}
