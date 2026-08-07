namespace EventPhoto.Contracts.Responses.Events;

/// <summary>
/// Comprehensive workspace response for the Event Workspace UI.
/// Combines core event data with live statistics to avoid multiple round-trips.
/// </summary>
public sealed record EventWorkspaceResponse(
    Guid Id,
    string Name,
    string? Description,
    string EventType,
    DateOnly EventDate,
    string? VenueName,
    string? ClientName,
    string WatchFolder,
    string ThumbnailFolder,
    string? QrCodeUrl,
    bool IsActive,
    int PhotoCount,
    long TotalSizeBytes,
    string TotalSize,
    int TotalDownloads,
    DateTimeOffset CreatedAt,
    int? GalleryRecentCount,
    // Gallery access
    bool AllowGalleryBrowsing,
    bool AllowFaceSearch,
    bool RestrictDownloadsToMatchedPhotos,
    // Face recognition
    bool EnableFaceRecognition,
    float FaceMatchThreshold,
    // Watermark quick-check
    bool WatermarkEnabled);
