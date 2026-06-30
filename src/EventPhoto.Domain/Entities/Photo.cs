using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Events;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Represents metadata for a single photo in an event.
/// </summary>
public sealed class Photo : AggregateRoot
{
    private Photo()
    {
    }

    /// <summary>
    /// Gets the identifier of the event this photo belongs to.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the absolute path to the original full-resolution photo.
    /// </summary>
    public string OriginalPath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the absolute path to the generated thumbnail.
    /// </summary>
    public string ThumbnailPath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>
    /// Gets the MIME type.
    /// </summary>
    public string MimeType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    public int? Width { get; private set; }

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    public int? Height { get; private set; }

    /// <summary>
    /// Gets the EXIF capture date if available.
    /// </summary>
    public DateTimeOffset? TakenAt { get; private set; }

    /// <summary>
    /// Gets when the file was detected by the file watcher.
    /// </summary>
    public DateTimeOffset CapturedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the number of times this photo has been downloaded.
    /// </summary>
    public int DownloadCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this photo has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the thumbnail generation status.
    /// </summary>
    public ThumbnailStatus ThumbnailStatus { get; private set; } = ThumbnailStatus.Pending;

    /// <summary>
    /// Gets the navigation property to the parent event.
    /// </summary>
    public Event? Event { get; private set; }

    /// <summary>
    /// Creates a new <see cref="Photo"/> entity when a file is detected.
    /// </summary>
    /// <param name="eventId">The parent event identifier.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="originalPath">The full-resolution file path.</param>
    /// <param name="thumbnailPath">The thumbnail output path.</param>
    /// <param name="fileSizeBytes">The file size in bytes.</param>
    /// <param name="mimeType">The MIME type.</param>
    /// <param name="takenAt">The optional EXIF capture timestamp.</param>
    /// <returns>A new <see cref="Photo"/> instance.</returns>
    public static Photo Create(
        Guid eventId,
        string fileName,
        string originalPath,
        string thumbnailPath,
        long fileSizeBytes,
        string mimeType,
        DateTimeOffset? takenAt = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException("EventId is required.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("File name is required.");
        }

        if (string.IsNullOrWhiteSpace(originalPath))
        {
            throw new DomainException("Original path is required.");
        }

        if (string.IsNullOrWhiteSpace(thumbnailPath))
        {
            throw new DomainException("Thumbnail path is required.");
        }

        if (fileSizeBytes < 0)
        {
            throw new DomainException("File size cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new DomainException("MIME type is required.");
        }

        var photo = new Photo
        {
            EventId = eventId,
            FileName = fileName.Trim(),
            OriginalPath = Path.GetFullPath(originalPath),
            ThumbnailPath = Path.GetFullPath(thumbnailPath),
            FileSizeBytes = fileSizeBytes,
            MimeType = mimeType.Trim(),
            TakenAt = takenAt
        };

        photo.RaiseDomainEvent(new PhotoCreatedEvent(photo.Id, eventId, photo.FileName));
        return photo;
    }

    /// <summary>
    /// Marks thumbnail generation as in progress.
    /// </summary>
    public void MarkThumbnailProcessing()
    {
        ThumbnailStatus = ThumbnailStatus.Processing;
        Touch();
    }

    /// <summary>
    /// Marks thumbnail generation as completed.
    /// </summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    public void MarkThumbnailDone(int width, int height)
    {
        if (width <= 0)
        {
            throw new DomainException("Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new DomainException("Height must be greater than zero.");
        }

        ThumbnailStatus = ThumbnailStatus.Done;
        Width = width;
        Height = height;
        Touch();
    }

    /// <summary>
    /// Marks thumbnail generation as failed.
    /// </summary>
    public void MarkThumbnailFailed()
    {
        ThumbnailStatus = ThumbnailStatus.Failed;
        Touch();
    }

    /// <summary>
    /// Records a download event for this photo.
    /// </summary>
    public void RecordDownload()
    {
        DownloadCount++;
        Touch();
    }

    /// <summary>
    /// Soft-deletes this photo.
    /// </summary>
    public void Delete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        Touch();
        RaiseDomainEvent(new PhotoDeletedEvent(Id, EventId));
    }
}
