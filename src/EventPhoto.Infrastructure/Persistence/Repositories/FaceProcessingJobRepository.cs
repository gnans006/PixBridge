using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL implementation of <see cref="IFaceProcessingJobRepository"/>.
/// </summary>
public sealed class FaceProcessingJobRepository(AppDbContext context) : IFaceProcessingJobRepository
{
    /// <inheritdoc />
    public async Task AddAsync(FaceProcessingJob job, CancellationToken cancellationToken = default)
        => await context.FaceProcessingJobs.AddAsync(job, cancellationToken);

    /// <inheritdoc />
    public Task<FaceProcessingJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
        => context.FaceProcessingJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

    /// <inheritdoc />
    public Task<FaceProcessingJob?> GetActiveByPhotoIdAsync(Guid photoId, CancellationToken cancellationToken = default)
        => context.FaceProcessingJobs
            .Where(j => j.PhotoId == photoId
                && j.Status != FaceJobStatus.Completed
                && j.Status != FaceJobStatus.DeadLettered
                && j.Status != FaceJobStatus.Ignored)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(FaceProcessingJob job, CancellationToken cancellationToken = default)
    {
        context.FaceProcessingJobs.Update(job);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<List<FaceProcessingJob>> GetPendingBatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
        => context.FaceProcessingJobs
            .Where(j => j.Status == FaceJobStatus.Pending)
            .OrderBy(j => j.Priority)
            .ThenBy(j => j.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<FaceProcessingJob>> GetRetryEligibleBatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return context.FaceProcessingJobs
            .Where(j => j.Status == FaceJobStatus.Failed
                && j.NextRetryAt != null
                && j.NextRetryAt <= now)
            .OrderBy(j => j.NextRetryAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(List<FaceProcessingJob> Items, int TotalCount)> GetDeadLetteredPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.FaceProcessingJobs
            .Where(j => j.Status == FaceJobStatus.DeadLettered)
            .OrderByDescending(j => j.UpdatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    public Task<List<FaceProcessingJob>> GetDeadLetteredByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
        => context.FaceProcessingJobs
            .Where(j => j.EventId == eventId && j.Status == FaceJobStatus.DeadLettered)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Dictionary<FaceJobStatus, int>> GetStatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var counts = await context.FaceProcessingJobs
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.Status, c => c.Count);
    }

    /// <inheritdoc />
    public Task<int> GetQueueDepthAsync(CancellationToken cancellationToken = default)
        => context.FaceProcessingJobs
            .CountAsync(j =>
                j.Status == FaceJobStatus.Pending ||
                j.Status == FaceJobStatus.Queued ||
                j.Status == FaceJobStatus.Detecting ||
                j.Status == FaceJobStatus.QualityChecking ||
                j.Status == FaceJobStatus.Embedding ||
                j.Status == FaceJobStatus.Indexing ||
                j.Status == FaceJobStatus.Failed,
            cancellationToken);

    /// <inheritdoc />
    public async Task<Dictionary<Guid, (int Pending, int Failed, int Completed, int DeadLettered)>>
        GetEventJobCountsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default)
    {
        var ids = eventIds.ToList();
        var rows = await context.FaceProcessingJobs
            .Where(j => ids.Contains(j.EventId))
            .GroupBy(j => new { j.EventId, j.Status })
            .Select(g => new { g.Key.EventId, g.Key.Status, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, (int Pending, int Failed, int Completed, int DeadLettered)>();
        foreach (var id in ids)
            result[id] = (0, 0, 0, 0);

        foreach (var row in rows)
        {
            var (p, f, c, d) = result[row.EventId];
            result[row.EventId] = row.Status switch
            {
                FaceJobStatus.Pending or FaceJobStatus.Queued => (p + row.Count, f, c, d),
                FaceJobStatus.Failed => (p, f + row.Count, c, d),
                FaceJobStatus.Completed => (p, f, c + row.Count, d),
                FaceJobStatus.DeadLettered => (p, f, c, d + row.Count),
                _ => (p, f, c, d)
            };
        }

        return result;
    }
}
