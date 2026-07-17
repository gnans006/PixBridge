namespace EventPhoto.Domain.Enums;

/// <summary>
/// Represents the face-indexing lifecycle of a photo.
/// </summary>
public enum FaceIndexStatus
{
    /// <summary>Face recognition is disabled for the parent event — no indexing required.</summary>
    NotRequired = 0,

    /// <summary>Photo is queued and waiting for the FaceIndexingService to pick it up.</summary>
    Pending = 1,

    /// <summary>The FaceIndexingService is actively processing this photo.</summary>
    Processing = 2,

    /// <summary>All faces have been detected and their embeddings stored successfully.</summary>
    Completed = 3,

    /// <summary>Face indexing failed after all retry attempts have been exhausted.</summary>
    Failed = 4
}
