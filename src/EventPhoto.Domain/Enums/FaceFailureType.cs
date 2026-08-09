namespace EventPhoto.Domain.Enums;

/// <summary>
/// Categorises the type of failure that occurred during AI face processing.
/// Used to determine retry eligibility and dead letter routing.
/// </summary>
public enum FaceFailureType
{
    /// <summary>No face was detected in the photo — not a failure; completes successfully with zero embeddings.</summary>
    NoFaceDetected = 0,

    /// <summary>Image file is corrupt, unreadable, or in an unsupported format. Permanent failure — no retry.</summary>
    CorruptedImage = 1,

    /// <summary>PostgreSQL was unreachable during the indexing write. Transient — retryable.</summary>
    DatabaseUnavailable = 2,

    /// <summary>The Python face-recognition service returned an error or timed out. Transient — retryable.</summary>
    EmbeddingServiceFailure = 3,

    /// <summary>The original photo file could not be read from storage. Transient — retryable.</summary>
    StorageUnavailable = 4,

    /// <summary>Face quality score fell below the configured minimum threshold. Non-retryable by default.</summary>
    QualityRejected = 5,

    /// <summary>An unexpected runtime exception occurred. Transient — retryable up to max retries.</summary>
    UnexpectedError = 6
}
