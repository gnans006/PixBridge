namespace EventPhoto.Domain.Interfaces;

/// <summary>
/// Unit of Work that coordinates committing changes across repositories.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits changes directly to the database WITHOUT dispatching domain events.
    /// Use ONLY inside domain-event handlers to avoid recursive dispatch.
    /// </summary>
    Task<int> SaveAuditAsync(CancellationToken cancellationToken = default);
}
