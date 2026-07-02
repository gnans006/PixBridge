using EventPhoto.Contracts.Responses.Statistics;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Statistics.Queries;

/// <summary>Query that returns aggregate dashboard statistics across all events.</summary>
public sealed record GetDashboardStatsQuery : IRequest<Result<DashboardStatsResponse>>;

/// <summary>Handles <see cref="GetDashboardStatsQuery"/>.</summary>
public sealed class GetDashboardStatsQueryHandler(
    IEventRepository eventRepository,
    IDownloadLogRepository downloadLogRepository)
    : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<DashboardStatsResponse>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var allEvents = await eventRepository.GetAllAsync(cancellationToken);
        var activeEvents = allEvents.Where(e => e.IsActive).ToList();
        var totalPhotos = allEvents.Sum(e => e.PhotoCount);
        var totalSizeBytes = allEvents.Sum(e => e.TotalSizeBytes);
        var totalDownloads = await downloadLogRepository.GetTotalCountAsync(cancellationToken);
        var sizeHuman = totalSizeBytes switch
        {
            < 1024 => $"{totalSizeBytes} B",
            < 1_048_576 => $"{totalSizeBytes / 1024.0:F1} KB",
            < 1_073_741_824 => $"{totalSizeBytes / 1_048_576.0:F1} MB",
            _ => $"{totalSizeBytes / 1_073_741_824.0:F2} GB"
        };

        return Result.Success(new DashboardStatsResponse(
            allEvents.Count,
            activeEvents.Count,
            totalPhotos,
            totalDownloads,
            totalSizeBytes,
            sizeHuman));
    }
}
