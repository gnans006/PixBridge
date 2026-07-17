using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IPhotoRepository"/>.
/// </summary>
public sealed class PhotoRepository(AppDbContext context) : IPhotoRepository
{
    /// <inheritdoc />
    public Task<Photo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Photos
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<List<Photo>> GetByEventIdAsync(
        Guid eventId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => context.Photos
            .Where(p => p.EventId == eventId && !p.IsDeleted)
            .OrderByDescending(p => p.CapturedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<Photo>> GetPendingThumbnailsAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
        => context.Photos
            .Where(p => p.ThumbnailStatus == ThumbnailStatus.Pending && !p.IsDeleted)
            .OrderBy(p => p.CapturedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> GetTotalCountByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
        => context.Photos
            .CountAsync(p => p.EventId == eventId && !p.IsDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Photo photo, CancellationToken cancellationToken = default)
        => await context.Photos.AddAsync(photo, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(Photo photo, CancellationToken cancellationToken = default)
    {
        context.Photos.Update(photo);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsByPathAsync(
        string originalPath,
        CancellationToken cancellationToken = default)
        => context.Photos
            .AnyAsync(p => p.OriginalPath == originalPath && !p.IsDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<List<Photo>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return context.Photos
            .Where(p => idList.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<Photo>> GetPendingFaceIndexAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
        => context.Photos
            .Where(p => p.FaceIndexStatus == Domain.Enums.FaceIndexStatus.Pending && !p.IsDeleted)
            .OrderBy(p => p.CapturedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
}
