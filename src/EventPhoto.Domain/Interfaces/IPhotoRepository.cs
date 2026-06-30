using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>
/// Repository contract for the <see cref="Photo"/> aggregate.
/// </summary>
public interface IPhotoRepository
{
    /// <summary>
    /// Gets a photo by identifier.
    /// </summary>
    /// <param name="id">The photo identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching photo, or <see langword="null"/> if not found.</returns>
    Task<Photo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paged photos for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of photos for the requested page.</returns>
    Task<List<Photo>> GetByEventIdAsync(Guid eventId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a batch of photos whose thumbnails are still pending.
    /// </summary>
    /// <param name="batchSize">The maximum batch size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of photos awaiting thumbnail processing.</returns>
    Task<List<Photo>> GetPendingThumbnailsAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total photo count for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total number of photos in the event.</returns>
    Task<int> GetTotalCountByEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new photo.
    /// </summary>
    /// <param name="photo">The photo to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(Photo photo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing photo.
    /// </summary>
    /// <param name="photo">The photo to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(Photo photo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a photo already exists for the provided original path.
    /// </summary>
    /// <param name="originalPath">The original file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when a photo exists for the path; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByPathAsync(string originalPath, CancellationToken cancellationToken = default);
}
