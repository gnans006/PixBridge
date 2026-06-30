using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IDownloadLogRepository"/>.
/// </summary>
public sealed class DownloadLogRepository(AppDbContext context) : IDownloadLogRepository
{
    /// <inheritdoc />
    public async Task AddAsync(DownloadLog log, CancellationToken cancellationToken = default)
        => await context.DownloadLogs.AddAsync(log, cancellationToken);

    /// <inheritdoc />
    public Task<int> GetDownloadCountByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
        => context.DownloadLogs.CountAsync(d => d.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public Task<int> GetDownloadCountByPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken = default)
        => context.DownloadLogs.CountAsync(d => d.PhotoId == photoId, cancellationToken);

    /// <inheritdoc />
    public Task<List<DownloadLog>> GetRecentByEventAsync(
        Guid eventId,
        int count,
        CancellationToken cancellationToken = default)
        => context.DownloadLogs
            .Where(d => d.EventId == eventId)
            .OrderByDescending(d => d.DownloadedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
}
