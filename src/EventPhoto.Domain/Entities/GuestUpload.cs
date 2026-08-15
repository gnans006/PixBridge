using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// A single photo submitted by a guest through a <see cref="GuestUploadSession"/>.
/// Studio staff must approve or reject the upload before it appears in the gallery.
/// </summary>
public sealed class GuestUpload : Entity
{
    private GuestUpload() { }

    /// <summary>Gets the event this upload belongs to.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Gets the guest upload session through which this photo was submitted.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>Gets the original file name as provided by the uploader.</summary>
    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>Gets the absolute server path where the file is stored.</summary>
    public string StoredPath { get; private set; } = string.Empty;

    /// <summary>Gets the absolute server path to the generated thumbnail, if available.</summary>
    public string? ThumbnailPath { get; private set; }

    /// <summary>Gets the file size in bytes.</summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>Gets the MIME type of the uploaded file.</summary>
    public string ContentType { get; private set; } = "image/jpeg";

    /// <summary>Gets the UTC timestamp when the file was submitted.</summary>
    public DateTimeOffset UploadedAt { get; private set; }

    /// <summary>Gets the current moderation status.</summary>
    public ModerationStatus ModerationStatus { get; private set; }

    /// <summary>Gets the rejection reason provided by the studio when rejecting this upload.</summary>
    public string? RejectionReason { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>Creates a new guest upload record in <see cref="Enums.ModerationStatus.Pending"/> state.</summary>
    public static GuestUpload Create(
        Guid eventId,
        Guid sessionId,
        string originalFileName,
        string storedPath,
        long fileSizeBytes,
        string contentType = "image/jpeg")
    {
        return new GuestUpload
        {
            EventId          = eventId,
            SessionId        = sessionId,
            OriginalFileName = originalFileName,
            StoredPath       = storedPath,
            FileSizeBytes    = fileSizeBytes,
            ContentType      = contentType,
            UploadedAt       = DateTimeOffset.UtcNow,
            ModerationStatus = ModerationStatus.Pending,
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>Approves this upload, making it eligible for gallery inclusion.</summary>
    public void Approve()
    {
        ModerationStatus = ModerationStatus.Approved;
        RejectionReason  = null;
        Touch();
    }

    /// <summary>Rejects this upload with an optional reason.</summary>
    public void Reject(string? reason = null)
    {
        ModerationStatus = ModerationStatus.Rejected;
        RejectionReason  = reason?.Trim();
        Touch();
    }

    /// <summary>Sets the thumbnail path once it has been generated.</summary>
    public void SetThumbnailPath(string path)
    {
        ThumbnailPath = path;
        Touch();
    }
}
