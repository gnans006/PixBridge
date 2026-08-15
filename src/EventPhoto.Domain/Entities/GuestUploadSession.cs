using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// A studio-controlled upload session that allows guests to submit their own photos for an event.
/// Each session has a unique 8-character code that guests use to access the upload page.
/// </summary>
public sealed class GuestUploadSession : Entity
{
    private GuestUploadSession() { }

    /// <summary>Gets the identifier of the event this session belongs to.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Gets the short unique code guests use to access the upload page (8 alphanumeric chars).</summary>
    public string SessionCode { get; private set; } = string.Empty;

    /// <summary>Gets an optional label for this upload session (e.g. "Wedding Guests").</summary>
    public string? Title { get; private set; }

    /// <summary>Gets the number of photos currently submitted through this session.</summary>
    public int PhotoCount { get; private set; }

    /// <summary>Gets the current lifecycle status of this session.</summary>
    public GuestUploadSessionStatus Status { get; private set; }

    /// <summary>Gets the UTC timestamp when this session was closed, if applicable.</summary>
    public DateTimeOffset? ClosedAt { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>Creates a new active guest upload session for the given event.</summary>
    public static GuestUploadSession Create(Guid eventId, string? title = null)
    {
        return new GuestUploadSession
        {
            EventId    = eventId,
            SessionCode = GenerateCode(),
            Title      = title?.Trim(),
            PhotoCount = 0,
            Status     = GuestUploadSessionStatus.Active,
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>Closes this session. No further uploads will be accepted.</summary>
    public void Close()
    {
        if (Status == GuestUploadSessionStatus.Closed)
            return;

        Status    = GuestUploadSessionStatus.Closed;
        ClosedAt  = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>Increments the submitted photo count by 1.</summary>
    public void IncrementPhotoCount()
    {
        PhotoCount++;
        Touch();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateCode()
        => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
