namespace EventPhoto.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a guest face-search session.
/// </summary>
public enum FaceSessionStatus
{
    /// <summary>Session created; selfie uploaded but vector search not yet started.</summary>
    Created = 0,

    /// <summary>Vector similarity search is in progress.</summary>
    Searching = 1,

    /// <summary>Search completed successfully; matched photos are available.</summary>
    Completed = 2,

    /// <summary>Session has expired and is no longer valid for downloads.</summary>
    Expired = 3
}
