namespace EventPhoto.Domain.Enums;

/// <summary>Defines the default gallery experience mode for new events.</summary>
public enum GalleryMode
{
    /// <summary>Guests can browse all photos freely.</summary>
    GalleryOnly = 0,

    /// <summary>Guests must use face search to find their own photos.</summary>
    FaceSearchOnly = 1,

    /// <summary>Guests can browse the gallery or use face search.</summary>
    Hybrid = 2,
}
