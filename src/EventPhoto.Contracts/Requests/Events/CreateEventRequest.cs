namespace EventPhoto.Contracts.Requests.Events;

/// <summary>Request model for creating a new photography event.</summary>
public sealed record CreateEventRequest(
    string Name,
    string EventType,
    DateOnly EventDate,
    string WatchFolder,
    string? Description,
    string? VenueName,
    string? ClientName,
    int? GalleryRecentCount,
    // Face Recognition
    bool EnableFaceRecognition = false,
    bool AllowGalleryBrowsing = true,
    bool AllowFaceSearch = false,
    bool RestrictDownloadsToMatchedPhotos = false,
    float FaceMatchThreshold = 0.75f);
