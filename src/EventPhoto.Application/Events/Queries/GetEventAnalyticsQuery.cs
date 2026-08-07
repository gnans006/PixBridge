using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Events.Queries;

/// <summary>Returns analytics data for a single event's workspace analytics tab.</summary>
/// <param name="EventId">The event identifier.</param>
public sealed record GetEventAnalyticsQuery(Guid EventId)
    : IRequest<Result<EventAnalyticsResponse>>;

/// <summary>Handles <see cref="GetEventAnalyticsQuery"/>.</summary>
public sealed class GetEventAnalyticsQueryHandler(
    IEventRepository eventRepository,
    IDownloadLogRepository downloadLogRepository,
    IFileStorageService fileStorageService)
    : IRequestHandler<GetEventAnalyticsQuery, Result<EventAnalyticsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EventAnalyticsResponse>> Handle(
        GetEventAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<EventAnalyticsResponse>($"Event '{request.EventId}' was not found.");
        }

        var totalDownloads = await downloadLogRepository.GetDownloadCountByEventAsync(
            request.EventId, cancellationToken);

        // Get recent downloads to compute daily chart data (up to 1000 most recent)
        var recentLogs = await downloadLogRepository.GetRecentByEventAsync(
            request.EventId, 1000, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayCount = recentLogs.Count(l =>
            DateOnly.FromDateTime(l.DownloadedAt.UtcDateTime) == today);

        // Build 30-day daily aggregation
        var cutoffDate = today.AddDays(-29);
        var dailyMap = recentLogs
            .Where(l => DateOnly.FromDateTime(l.DownloadedAt.UtcDateTime) >= cutoffDate)
            .GroupBy(l => DateOnly.FromDateTime(l.DownloadedAt.UtcDateTime))
            .ToDictionary(g => g.Key, g => g.Count());

        var dailyDownloads = Enumerable.Range(0, 30)
            .Select(i => cutoffDate.AddDays(i))
            .Select(date => new DailyDownloadCount(date, dailyMap.GetValueOrDefault(date, 0)))
            .ToList();

        // Recent activity (last 20)
        var recentActivity = recentLogs
            .Take(20)
            .Select(l => new RecentDownloadItem(l.PhotoId, l.IpAddress, l.DownloadedAt))
            .ToList();

        var liveSizeBytes = fileStorageService.GetFolderSize(eventEntity.WatchFolder);
        var sizeBytes = liveSizeBytes > 0 ? liveSizeBytes : eventEntity.TotalSizeBytes;

        return Result.Success(new EventAnalyticsResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.PhotoCount,
            totalDownloads,
            todayCount,
            sizeBytes,
            FormatBytes(sizeBytes),
            dailyDownloads,
            recentActivity));
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        <= 0 => "0 B",
        < 1024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1024.0:F1} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
        _ => $"{bytes / 1_073_741_824.0:F2} GB",
    };
}
