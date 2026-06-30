namespace EventPhoto.Domain.Enums;

/// <summary>
/// Processing status of a photo thumbnail.
/// </summary>
public enum ThumbnailStatus
{
    /// <summary>
    /// Thumbnail generation has not started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Thumbnail generation is in progress.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Thumbnail generation completed successfully.
    /// </summary>
    Done = 2,

    /// <summary>
    /// Thumbnail generation failed.
    /// </summary>
    Failed = 3
}
