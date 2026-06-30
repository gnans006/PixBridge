using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>
/// Repository contract for the <see cref="Event"/> aggregate.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Gets an event by identifier.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching event, or <see langword="null"/> if not found.</returns>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active, non-deleted events.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of active events.</returns>
    Task<List<Event>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an event with its photos loaded.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The event with photos, or <see langword="null"/> if not found.</returns>
    Task<Event?> GetWithPhotosAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new event.
    /// </summary>
    /// <param name="eventEntity">The event to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(Event eventEntity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventEntity">The event to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(Event eventEntity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an event exists.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the event exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
