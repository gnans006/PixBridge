using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Events;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

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
        string? clientName = null)
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
            ClientName = clientName?.Trim()
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
    public void Update(
        string name,
        EventType eventType,
        DateOnly eventDate,
        string? description,
        string? venueName,
        string? clientName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Event name is required.");
        }

        Name = name.Trim();
        EventType = eventType;
        EventDate = eventDate;
        Description = description?.Trim();
        VenueName = venueName?.Trim();
        ClientName = clientName?.Trim();
        Touch();
    }
}
