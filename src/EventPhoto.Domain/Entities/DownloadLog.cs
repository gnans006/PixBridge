using EventPhoto.Domain.Common;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Records each time a guest downloads a photo.
/// </summary>
public sealed class DownloadLog : Entity
{
    private DownloadLog()
    {
    }

    /// <summary>
    /// Gets the identifier of the photo that was downloaded.
    /// </summary>
    public Guid PhotoId { get; private set; }

    /// <summary>
    /// Gets the identifier of the event the photo belongs to.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Gets the IP address of the downloader when available.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Gets the browser user-agent string.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Gets when the download occurred.
    /// </summary>
    public DateTimeOffset DownloadedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the navigation property to the photo.
    /// </summary>
    public Photo? Photo { get; private set; }

    /// <summary>
    /// Gets the navigation property to the event.
    /// </summary>
    public Event? Event { get; private set; }

    /// <summary>
    /// Creates a download log entry.
    /// </summary>
    /// <param name="photoId">The downloaded photo identifier.</param>
    /// <param name="eventId">The parent event identifier.</param>
    /// <param name="ipAddress">The optional IP address.</param>
    /// <param name="userAgent">The optional user-agent value.</param>
    /// <returns>A new <see cref="DownloadLog"/> instance.</returns>
    public static DownloadLog Create(Guid photoId, Guid eventId, string? ipAddress, string? userAgent)
    {
        if (photoId == Guid.Empty)
        {
            throw new DomainException("PhotoId is required.");
        }

        if (eventId == Guid.Empty)
        {
            throw new DomainException("EventId is required.");
        }

        return new DownloadLog
        {
            PhotoId = photoId,
            EventId = eventId,
            IpAddress = ipAddress?.Length > 50 ? ipAddress[..50] : ipAddress,
            UserAgent = userAgent
        };
    }
}
