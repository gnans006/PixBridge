namespace EventPhoto.Contracts.Responses.Statistics;

/// <summary>A single item in the recent-activity timeline.</summary>
public sealed record RecentActivityItemResponse(
    string ActivityType,
    Guid EventId,
    string EventName,
    Guid? PhotoId,
    DateTimeOffset OccurredAt,
    string? IpAddress);
