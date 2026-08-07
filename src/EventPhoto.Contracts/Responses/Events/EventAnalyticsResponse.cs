namespace EventPhoto.Contracts.Responses.Events;

/// <summary>A single day's download count for a sparkline / bar chart.</summary>
public sealed record DailyDownloadCount(DateOnly Date, int Count);

/// <summary>Analytics response for a single event's workspace analytics tab.</summary>
public sealed record EventAnalyticsResponse(
    Guid EventId,
    string EventName,
    int TotalPhotos,
    int TotalDownloads,
    int TodayDownloads,
    long StorageSizeBytes,
    string StorageHuman,
    List<DailyDownloadCount> DownloadsLast30Days,
    List<RecentDownloadItem> RecentActivity);

/// <summary>A recent download activity item.</summary>
public sealed record RecentDownloadItem(
    Guid PhotoId,
    string? IpAddress,
    DateTimeOffset DownloadedAt);
