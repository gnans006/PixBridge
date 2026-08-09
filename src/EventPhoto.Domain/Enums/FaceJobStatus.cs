namespace EventPhoto.Domain.Enums;

/// <summary>
/// Represents the complete lifecycle of a <see cref="Entities.FaceProcessingJob"/>.
/// </summary>
public enum FaceJobStatus
{
    /// <summary>Job created, waiting to be picked up by the AI Discovery Pipeline.</summary>
    Pending = 0,

    /// <summary>Job has been loaded into the in-memory priority channel.</summary>
    Queued = 1,

    /// <summary>Face detection is in progress against the Python service.</summary>
    Detecting = 2,

    /// <summary>Quality validation is being evaluated against configured thresholds.</summary>
    QualityChecking = 3,

    /// <summary>ArcFace embedding generation is in progress.</summary>
    Embedding = 4,

    /// <summary>Vector is being written to the pgvector HNSW index.</summary>
    Indexing = 5,

    /// <summary>Processing completed successfully — photo is search-ready.</summary>
    Completed = 6,

    /// <summary>Processing failed — within retry window; will be retried.</summary>
    Failed = 7,

    /// <summary>Maximum retries exhausted — moved to dead letter queue for manual review.</summary>
    DeadLettered = 8,

    /// <summary>Job was ignored by studio operator (dead letter explicit discard).</summary>
    Ignored = 9
}
