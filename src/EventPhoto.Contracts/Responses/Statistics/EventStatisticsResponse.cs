namespace EventPhoto.Contracts.Responses.Statistics;

/// <summary>Statistics summary for a photography event.</summary>
public sealed record EventStatisticsResponse(
    Guid EventId,
    string EventName,
    int TotalPhotos,
    int TotalDownloads,
    long TotalSizeBytes,
    string TotalSizeHuman,
    int ThumbnailsPending,
    int ThumbnailsFailed,
    DateTimeOffset? LastPhotoAt);
