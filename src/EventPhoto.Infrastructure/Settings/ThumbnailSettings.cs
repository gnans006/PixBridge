namespace EventPhoto.Infrastructure.Settings;

/// <summary>
/// Configuration settings for thumbnail generation.
/// Bound from the <c>Thumbnails</c> configuration section.
/// </summary>
public sealed class ThumbnailSettings
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Thumbnails";

    /// <summary>
    /// Gets or sets the maximum thumbnail width in pixels. Defaults to 400.
    /// </summary>
    public int MaxWidth { get; set; } = 400;

    /// <summary>
    /// Gets or sets the maximum thumbnail height in pixels. Defaults to 300.
    /// </summary>
    public int MaxHeight { get; set; } = 300;
}
