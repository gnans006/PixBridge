using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for <see cref="FaceProcessingJob"/> persistence.</summary>
public interface IFaceProcessingJobRepository
{
    /// <summary>Adds a new job to the queue.</summary>
    Task AddAsync(FaceProcessingJob job, CancellationToken cancellationToken = default);

    /// <summary>Returns the job by its identifier.</summary>
    Task<FaceProcessingJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>Returns the active (non-completed, non-dead-lettered) job for a given photo, if any.</summary>
    Task<FaceProcessingJob?> GetActiveByPhotoIdAsync(Guid photoId, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing job.</summary>
    Task UpdateAsync(FaceProcessingJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a batch of pending jobs ordered by priority (lowest = highest priority).
    /// Only jobs with <c>Status == Pending</c> and no <c>NextRetryAt</c> constraint are returned.
    /// </summary>
    Task<List<FaceProcessingJob>> GetPendingBatchAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a batch of failed jobs whose <c>NextRetryAt</c> has elapsed and are due for retry.
    /// </summary>
    Task<List<FaceProcessingJob>> GetRetryEligibleBatchAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Returns paged dead-lettered jobs for the AI Studio dashboard.</summary>
    Task<(List<FaceProcessingJob> Items, int TotalCount)> GetDeadLetteredPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Returns dead-lettered jobs for a specific event.</summary>
    Task<List<FaceProcessingJob>> GetDeadLetteredByEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Returns queue statistics grouped by status.</summary>
    Task<Dictionary<FaceJobStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the total pending + queued + in-progress jobs (queue depth).</summary>
    Task<int> GetQueueDepthAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns per-event job counts for the AI health panel.</summary>
    Task<Dictionary<Guid, (int Pending, int Failed, int Completed, int DeadLettered)>>
        GetEventJobCountsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default);
}
