namespace EventPhoto.Contracts.Responses.Statistics;

/// <summary>Per-event storage breakdown item.</summary>
public sealed record StorageEventItem(
    Guid EventId,
    string EventName,
    long SizeBytes,
    string SizeHuman,
    int PhotoCount);

/// <summary>Storage analytics for all events.</summary>
public sealed record StorageAnalyticsResponse(
    long TotalSizeBytes,
    string TotalSizeHuman,
    int EventCount,
    List<StorageEventItem> TopEvents);
