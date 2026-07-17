using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IPhotoMatchRepository"/>.
/// </summary>
public sealed class PhotoMatchRepository(AppDbContext context) : IPhotoMatchRepository
{
    /// <inheritdoc />
    public Task<List<PhotoMatch>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => context.PhotoMatches
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.SimilarityScore)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<PhotoMatch>> GetPagedBySessionIdAsync(
        Guid sessionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => context.PhotoMatches
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.SimilarityScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => context.PhotoMatches.CountAsync(m => m.SessionId == sessionId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsPhotoMatchedInSessionAsync(
        Guid sessionId,
        Guid photoId,
        CancellationToken cancellationToken = default)
        => context.PhotoMatches.AnyAsync(
            m => m.SessionId == sessionId && m.PhotoId == photoId,
            cancellationToken);

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<PhotoMatch> matches, CancellationToken cancellationToken = default)
        => await context.PhotoMatches.AddRangeAsync(matches, cancellationToken);
}
