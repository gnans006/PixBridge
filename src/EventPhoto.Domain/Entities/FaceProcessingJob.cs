using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Tracks a single photo's journey through the AI Discovery Pipeline.
/// One <see cref="FaceProcessingJob"/> is created per photo when it enters the pipeline.
///
/// <para>
/// This entity owns retry state, failure classification, exponential back-off scheduling,
/// and dead-letter promotion — keeping the <see cref="Photo"/> entity clean of operational concerns.
/// </para>
///
/// <para>Retry delays (exponential back-off):</para>
/// <list type="bullet">
///   <item>Attempt 1 → +1 min</item>
///   <item>Attempt 2 → +5 min</item>
///   <item>Attempt 3 → +15 min</item>
///   <item>Attempt 4 → +30 min</item>
///   <item>Attempt 5 → +60 min → dead-letter</item>
/// </list>
/// </summary>
public sealed class FaceProcessingJob : AggregateRoot
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromMinutes(60),
    ];

    private const int MaxRetries = 5;

    private FaceProcessingJob()
    {
    }

    /// <summary>Gets the parent event identifier.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Gets the photo being processed.</summary>
    public Guid PhotoId { get; private set; }

    /// <summary>Gets the current status in the processing state machine.</summary>
    public FaceJobStatus Status { get; private set; } = FaceJobStatus.Pending;

    /// <summary>Gets the number of retry attempts made so far.</summary>
    public int RetryCount { get; private set; }

    /// <summary>Gets the human-readable description of the most recent error.</summary>
    public string? LastError { get; private set; }

    /// <summary>Gets the classified failure type of the most recent failure, if any.</summary>
    public FaceFailureType? FailureType { get; private set; }

    /// <summary>Gets the UTC time after which the job is eligible for re-queuing (exponential back-off).</summary>
    public DateTimeOffset? NextRetryAt { get; private set; }

    /// <summary>Gets when the pipeline started processing this job.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Gets when the pipeline finished processing (success or dead-letter).</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Gets the processing priority (lower = higher priority).</summary>
    public int Priority { get; private set; } = 2;

    // Navigation properties
    /// <summary>Gets the parent event.</summary>
    public Event? Event { get; private set; }

    /// <summary>Gets the parent photo.</summary>
    public Photo? Photo { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="FaceProcessingJob"/> for a photo entering the AI Discovery Pipeline.
    /// </summary>
    /// <param name="eventId">Parent event identifier.</param>
    /// <param name="photoId">Photo to process.</param>
    /// <param name="priority">Processing priority (1=highest, 5=lowest).</param>
    public static FaceProcessingJob Create(Guid eventId, Guid photoId, int priority = 2)
    {
        if (eventId == Guid.Empty)
            throw new DomainException("EventId is required.");
        if (photoId == Guid.Empty)
            throw new DomainException("PhotoId is required.");

        return new FaceProcessingJob
        {
            EventId = eventId,
            PhotoId = photoId,
            Priority = priority,
            Status = FaceJobStatus.Pending
        };
    }

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>Marks the job as loaded into the in-memory priority channel.</summary>
    public void MarkQueued()
    {
        Status = FaceJobStatus.Queued;
        Touch();
    }

    /// <summary>Marks the pipeline as actively detecting faces.</summary>
    public void MarkDetecting()
    {
        Status = FaceJobStatus.Detecting;
        StartedAt ??= DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>Marks the pipeline as evaluating face quality.</summary>
    public void MarkQualityChecking()
    {
        Status = FaceJobStatus.QualityChecking;
        Touch();
    }

    /// <summary>Marks the pipeline as generating the ArcFace embedding.</summary>
    public void MarkEmbedding()
    {
        Status = FaceJobStatus.Embedding;
        Touch();
    }

    /// <summary>Marks the pipeline as writing the vector to the HNSW index.</summary>
    public void MarkIndexing()
    {
        Status = FaceJobStatus.Indexing;
        Touch();
    }

    /// <summary>
    /// Marks the job as successfully completed.
    /// Clears any previous failure state.
    /// </summary>
    public void MarkCompleted()
    {
        Status = FaceJobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        LastError = null;
        FailureType = null;
        NextRetryAt = null;
        Touch();
    }

    /// <summary>
    /// Records a failure and either schedules a retry (with exponential back-off)
    /// or promotes to dead-letter queue after <see cref="MaxRetries"/> attempts.
    /// </summary>
    /// <param name="errorMessage">Human-readable description of the error.</param>
    /// <param name="failureType">Classified failure type.</param>
    /// <returns><c>true</c> when promoted to dead-letter; <c>false</c> when scheduled for retry.</returns>
    public bool MarkFailed(string errorMessage, FaceFailureType failureType)
    {
        LastError = errorMessage;
        FailureType = failureType;

        // Permanent failures go straight to dead-letter
        var isPermanent = failureType is FaceFailureType.CorruptedImage or FaceFailureType.QualityRejected;
        if (isPermanent || RetryCount >= MaxRetries)
        {
            Status = FaceJobStatus.DeadLettered;
            CompletedAt = DateTimeOffset.UtcNow;
            Touch();
            return true;
        }

        RetryCount++;
        var delay = RetryCount <= RetryDelays.Length
            ? RetryDelays[RetryCount - 1]
            : RetryDelays[^1];

        NextRetryAt = DateTimeOffset.UtcNow.Add(delay);
        Status = FaceJobStatus.Failed;
        Touch();
        return false;
    }

    /// <summary>
    /// Promotes a dead-lettered job back to <see cref="FaceJobStatus.Pending"/> for manual retry.
    /// Resets retry count and clears back-off schedule.
    /// </summary>
    public void MarkRetryFromDeadLetter()
    {
        if (Status != FaceJobStatus.DeadLettered)
            throw new DomainException("Only dead-lettered jobs can be manually retried.");

        Status = FaceJobStatus.Pending;
        RetryCount = 0;
        LastError = null;
        FailureType = null;
        NextRetryAt = null;
        CompletedAt = null;
        Touch();
    }

    /// <summary>
    /// Marks a dead-lettered job as explicitly ignored by the studio operator.
    /// </summary>
    public void MarkIgnored()
    {
        if (Status != FaceJobStatus.DeadLettered)
            throw new DomainException("Only dead-lettered jobs can be ignored.");

        Status = FaceJobStatus.Ignored;
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Returns <c>true</c> when a failed job is eligible to be re-queued.
    /// </summary>
    public bool IsReadyForRetry =>
        Status == FaceJobStatus.Failed &&
        NextRetryAt.HasValue &&
        DateTimeOffset.UtcNow >= NextRetryAt.Value;
}
