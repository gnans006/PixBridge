using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>
/// Repository contract for <see cref="DownloadLog"/> entities.
/// </summary>
public interface IDownloadLogRepository
{
    /// <summary>
    /// Adds a download log entry.
    /// </summary>
    /// <param name="log">The log entry to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(DownloadLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total download count for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total event download count.</returns>
    Task<int> GetDownloadCountByEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total download count for a photo.
    /// </summary>
    /// <param name="photoId">The photo identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total photo download count.</returns>
    Task<int> GetDownloadCountByPhotoAsync(Guid photoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent download logs for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="count">The maximum number of records to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The recent download log entries.</returns>
    Task<List<DownloadLog>> GetRecentByEventAsync(Guid eventId, int count, CancellationToken cancellationToken = default);
}
