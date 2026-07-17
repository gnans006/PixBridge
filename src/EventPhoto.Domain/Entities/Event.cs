using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Events;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

// Gallery-mode derivation:
//   AllowGalleryBrowsing=true,  AllowFaceSearch=false → GalleryOnly
//   AllowGalleryBrowsing=false, AllowFaceSearch=true  → FaceSearchOnly
//   AllowGalleryBrowsing=true,  AllowFaceSearch=true  → Hybrid

/// <summary>
/// Represents a photography event such as a wedding, birthday, or corporate event.
/// </summary>
public sealed class Event : AggregateRoot
{
    private readonly List<Photo> _photos = [];

    private Event()
    {
    }

    /// <summary>
    /// Gets the display name of the event.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the type of event.
    /// </summary>
    public EventType EventType { get; private set; }

    /// <summary>
    /// Gets the date of the event.
    /// </summary>
    public DateOnly EventDate { get; private set; }

    /// <summary>
    /// Gets the venue name.
    /// </summary>
    public string? VenueName { get; private set; }

    /// <summary>
    /// Gets the client name.
    /// </summary>
    public string? ClientName { get; private set; }

    /// <summary>
    /// Gets the absolute path to the folder being watched for new photos.
    /// </summary>
    public string WatchFolder { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the absolute path to the folder where thumbnails are stored.
    /// </summary>
    public string ThumbnailFolder { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the absolute path to the generated QR code image file.
    /// </summary>
    public string? QrCodePath { get; private set; }

    /// <summary>
    /// Gets the URL encoded inside the QR code.
    /// </summary>
    public string? QrCodeUrl { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this event is active and accepting new photos.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether this event has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the maximum number of most-recent photos shown in the public gallery.
    /// When <c>null</c> all photos are displayed.
    /// </summary>
    public int? GalleryRecentCount { get; private set; }

    /// <summary>
    /// Gets the cached photo count.
    /// </summary>
    public int PhotoCount { get; private set; }

    /// <summary>
    /// Gets the cached total size of all photos in bytes.
    /// </summary>
    public long TotalSizeBytes { get; private set; }

    /// <summary>
    /// Gets the identifier of the admin user who created this event.
    /// </summary>
    public Guid CreatedBy { get; private set; }

    // ── Face Recognition settings ─────────────────────────────────────────────

    /// <summary>
    /// When <c>true</c>, the background FaceIndexingService will process photos
    /// for this event and face search is available.
    /// </summary>
    public bool EnableFaceRecognition { get; private set; }

    /// <summary>
    /// When <c>true</c>, guests may browse the full photo gallery without uploading a selfie.
    /// At least one of <see cref="AllowGalleryBrowsing"/> or <see cref="AllowFaceSearch"/> must be <c>true</c>.
    /// </summary>
    public bool AllowGalleryBrowsing { get; private set; } = true;

    /// <summary>
    /// When <c>true</c>, guests may use the selfie-based face-search feature.
    /// Requires <see cref="EnableFaceRecognition"/> to be <c>true</c>.
    /// </summary>
    public bool AllowFaceSearch { get; private set; }

    /// <summary>
    /// When <c>true</c>, guests who used face search may only download photos
    /// that were returned as matches for their selfie.
    /// </summary>
    public bool RestrictDownloadsToMatchedPhotos { get; private set; }

    /// <summary>
    /// Cosine-similarity threshold used during face matching (0.0–1.0).
    /// Photos with a similarity score at or above this value are returned as matches.
    /// Defaults to <c>0.75</c>.
    /// </summary>
    public float FaceMatchThreshold { get; private set; } = 0.75f;

    /// <summary>
    /// Gets the photos associated with this event.
    /// </summary>
    public IReadOnlyCollection<Photo> Photos => _photos.AsReadOnly();

    /// <summary>
    /// Creates a new photography event.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="eventDate">The event date.</param>
    /// <param name="watchFolder">The folder being watched for incoming photos.</param>
    /// <param name="thumbnailFolder">The folder where generated thumbnails are stored.</param>
    /// <param name="createdBy">The creating user identifier.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="venueName">The optional venue name.</param>
    /// <param name="clientName">The optional client name.</param>
    /// <param name="galleryRecentCount">The optional maximum number of recent photos to show in the gallery.</param>
    /// <param name="enableFaceRecognition">Whether to enable face recognition for this event.</param>
    /// <param name="allowGalleryBrowsing">Whether guests may browse the full gallery.</param>
    /// <param name="allowFaceSearch">Whether guests may use selfie-based face search.</param>
    /// <param name="restrictDownloadsToMatchedPhotos">Whether downloads are restricted to matched photos.</param>
    /// <param name="faceMatchThreshold">Cosine similarity threshold for face matching (0.0–1.0).</param>
    /// <returns>A new <see cref="Event"/> instance.</returns>
    public static Event Create(
        string name,
        EventType eventType,
        DateOnly eventDate,
        string watchFolder,
        string thumbnailFolder,
        Guid createdBy,
        string? description = null,
        string? venueName = null,
        string? clientName = null,
        int? galleryRecentCount = null,
        bool enableFaceRecognition = false,
        bool allowGalleryBrowsing = true,
        bool allowFaceSearch = false,
        bool restrictDownloadsToMatchedPhotos = false,
        float faceMatchThreshold = 0.75f)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Event name is required.");
        }

        if (createdBy == Guid.Empty)
        {
            throw new DomainException("CreatedBy user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(watchFolder))
        {
            throw new DomainException("Watch folder path is required.");
        }

        if (string.IsNullOrWhiteSpace(thumbnailFolder))
        {
            throw new DomainException("Thumbnail folder path is required.");
        }

        if (galleryRecentCount.HasValue && galleryRecentCount.Value < 1)
        {
            throw new DomainException("Gallery recent count must be at least 1 when specified.");
        }

        if (!allowGalleryBrowsing && !allowFaceSearch)
        {
            throw new DomainException("At least one of AllowGalleryBrowsing or AllowFaceSearch must be enabled.");
        }

        if (faceMatchThreshold is < 0.0f or > 1.0f)
        {
            throw new DomainException("FaceMatchThreshold must be between 0.0 and 1.0.");
        }

        if (allowFaceSearch && !enableFaceRecognition)
        {
            throw new DomainException("AllowFaceSearch requires EnableFaceRecognition to be true.");
        }

        var eventEntity = new Event
        {
            Name = name.Trim(),
            EventType = eventType,
            EventDate = eventDate,
            WatchFolder = Path.GetFullPath(watchFolder),
            ThumbnailFolder = Path.GetFullPath(thumbnailFolder),
            CreatedBy = createdBy,
            Description = description?.Trim(),
            VenueName = venueName?.Trim(),
            ClientName = clientName?.Trim(),
            GalleryRecentCount = galleryRecentCount,
            EnableFaceRecognition = enableFaceRecognition,
            AllowGalleryBrowsing = allowGalleryBrowsing,
            AllowFaceSearch = allowFaceSearch,
            RestrictDownloadsToMatchedPhotos = restrictDownloadsToMatchedPhotos,
            FaceMatchThreshold = faceMatchThreshold
        };

        eventEntity.RaiseDomainEvent(new EventCreatedEvent(eventEntity.Id, eventEntity.Name));
        return eventEntity;
    }

    /// <summary>
    /// Sets the QR code details after generation.
    /// </summary>
    /// <param name="qrCodePath">The QR code image path.</param>
    /// <param name="qrCodeUrl">The QR code URL.</param>
    public void SetQrCode(string qrCodePath, string qrCodeUrl)
    {
        if (string.IsNullOrWhiteSpace(qrCodePath))
        {
            throw new DomainException("QR code path is required.");
        }

        if (string.IsNullOrWhiteSpace(qrCodeUrl))
        {
            throw new DomainException("QR code URL is required.");
        }

        QrCodePath = Path.GetFullPath(qrCodePath);
        QrCodeUrl = qrCodeUrl;
        Touch();
    }

    /// <summary>
    /// Deactivates the event so no new photos are accepted.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch();
        RaiseDomainEvent(new EventDeactivatedEvent(Id));
    }

    /// <summary>
    /// Reactivates the event.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
    }

    /// <summary>
    /// Soft-deletes the event.
    /// </summary>
    public void Delete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Updates event statistics when a photo is added.
    /// </summary>
    /// <param name="photoSizeBytes">The photo size in bytes.</param>
    public void IncrementPhotoCount(long photoSizeBytes)
    {
        if (photoSizeBytes < 0)
        {
            throw new DomainException("Photo size cannot be negative.");
        }

        PhotoCount++;
        TotalSizeBytes += photoSizeBytes;
        Touch();
    }

    /// <summary>
    /// Updates event statistics when a photo is removed.
    /// </summary>
    /// <param name="photoSizeBytes">The photo size in bytes.</param>
    public void DecrementPhotoCount(long photoSizeBytes)
    {
        if (photoSizeBytes < 0)
        {
            throw new DomainException("Photo size cannot be negative.");
        }

        PhotoCount = Math.Max(0, PhotoCount - 1);
        TotalSizeBytes = Math.Max(0, TotalSizeBytes - photoSizeBytes);
        Touch();
    }

    /// <summary>
    /// Updates the event's editable details.
    /// </summary>
    /// <param name="name">The updated name.</param>
    /// <param name="eventType">The updated event type.</param>
    /// <param name="eventDate">The updated event date.</param>
    /// <param name="description">The updated description.</param>
    /// <param name="venueName">The updated venue name.</param>
    /// <param name="clientName">The updated client name.</param>
    /// <param name="galleryRecentCount">The updated maximum number of recent photos shown in the gallery.</param>
    /// <param name="enableFaceRecognition">Whether face recognition is enabled.</param>
    /// <param name="allowGalleryBrowsing">Whether gallery browsing is allowed.</param>
    /// <param name="allowFaceSearch">Whether face search is allowed.</param>
    /// <param name="restrictDownloadsToMatchedPhotos">Whether downloads are restricted to matched photos.</param>
    /// <param name="faceMatchThreshold">Cosine similarity threshold (0.0–1.0).</param>
    public void Update(
        string name,
        EventType eventType,
        DateOnly eventDate,
        string? description,
        string? venueName,
        string? clientName,
        int? galleryRecentCount,
        bool enableFaceRecognition = false,
        bool allowGalleryBrowsing = true,
        bool allowFaceSearch = false,
        bool restrictDownloadsToMatchedPhotos = false,
        float faceMatchThreshold = 0.75f)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Event name is required.");
        }

        if (galleryRecentCount.HasValue && galleryRecentCount.Value < 1)
        {
            throw new DomainException("Gallery recent count must be at least 1 when specified.");
        }

        if (!allowGalleryBrowsing && !allowFaceSearch)
        {
            throw new DomainException("At least one of AllowGalleryBrowsing or AllowFaceSearch must be enabled.");
        }

        if (faceMatchThreshold is < 0.0f or > 1.0f)
        {
            throw new DomainException("FaceMatchThreshold must be between 0.0 and 1.0.");
        }

        if (allowFaceSearch && !enableFaceRecognition)
        {
            throw new DomainException("AllowFaceSearch requires EnableFaceRecognition to be true.");
        }

        Name = name.Trim();
        EventType = eventType;
        EventDate = eventDate;
        Description = description?.Trim();
        VenueName = venueName?.Trim();
        ClientName = clientName?.Trim();
        GalleryRecentCount = galleryRecentCount;
        EnableFaceRecognition = enableFaceRecognition;
        AllowGalleryBrowsing = allowGalleryBrowsing;
        AllowFaceSearch = allowFaceSearch;
        RestrictDownloadsToMatchedPhotos = restrictDownloadsToMatchedPhotos;
        FaceMatchThreshold = faceMatchThreshold;
        Touch();
    }
}
