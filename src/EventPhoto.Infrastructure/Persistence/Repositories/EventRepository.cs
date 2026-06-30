using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IEventRepository"/>.
/// </summary>
public sealed class EventRepository(AppDbContext context) : IEventRepository
{
    /// <inheritdoc />
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Events
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<List<Event>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => context.Events
            .Where(e => e.IsActive && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Event?> GetWithPhotosAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Events
            .Include(e => e.Photos)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Event eventEntity, CancellationToken cancellationToken = default)
        => await context.Events.AddAsync(eventEntity, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(Event eventEntity, CancellationToken cancellationToken = default)
    {
        context.Events.Update(eventEntity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Events.AnyAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
}
