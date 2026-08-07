namespace EventPhoto.Contracts.Requests.Events;

/// <summary>Request to update the gallery access settings for an event.</summary>
public sealed record UpdateGallerySettingsRequest(
    bool AllowGalleryBrowsing,
    bool AllowFaceSearch,
    bool RestrictDownloadsToMatchedPhotos,
    int? GalleryRecentCount);
