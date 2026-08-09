using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>PostgreSQL implementation of <see cref="IFaceClusterRepository"/>.</summary>
public sealed class FaceClusterRepository(AppDbContext context) : IFaceClusterRepository
{
    public async Task AddAsync(FaceCluster cluster, CancellationToken cancellationToken = default)
        => await context.FaceClusters.AddAsync(cluster, cancellationToken);

    public Task UpdateAsync(FaceCluster cluster, CancellationToken cancellationToken = default)
    {
        context.FaceClusters.Update(cluster);
        return Task.CompletedTask;
    }

    public Task<List<FaceCluster>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        => context.FaceClusters
            .Where(c => c.EventId == eventId)
            .OrderByDescending(c => c.PhotoCount)
            .ToListAsync(cancellationToken);

    public Task<int> CountByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        => context.FaceClusters.CountAsync(c => c.EventId == eventId, cancellationToken);
}
