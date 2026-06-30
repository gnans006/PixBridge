namespace EventPhoto.Contracts.Responses.Photos;

/// <summary>Photo metadata returned by the API.</summary>
public sealed record PhotoResponse(
    Guid Id,
    Guid EventId,
    string FileName,
    string ThumbnailUrl,
    string OriginalUrl,
    long FileSizeBytes,
    int? Width,
    int? Height,
    DateTimeOffset? TakenAt,
    DateTimeOffset CapturedAt,
    int DownloadCount,
    string ThumbnailStatus);
