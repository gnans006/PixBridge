namespace EventPhoto.Contracts.Responses.Events;

/// <summary>Storage details for a single event's workspace storage tab.</summary>
public sealed record StorageMetricsResponse(
    Guid EventId,
    string WatchFolder,
    string ThumbnailFolder,
    long SizeBytes,
    string SizeHuman,
    int PhotoCount,
    int ThumbnailCount);
