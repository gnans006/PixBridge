namespace EventPhoto.Infrastructure.Services.Watermark;

/// <summary>
/// Strongly-typed configuration for the watermark disk cache.
/// Bound from the <c>Storage</c> section of <c>appsettings.json</c>.
/// </summary>
public sealed class WatermarkCacheOptions
{
    /// <summary>Configuration section key.</summary>
    public const string Section = "Storage";

    /// <summary>
    /// Absolute path to the root cache directory.
    /// Defaults to <c>{ContentRoot}\cache\watermarked</c> when not specified.
    /// </summary>
    public string? CachePath { get; set; }

    /// <summary>
    /// Maximum total cache size in bytes before LRU eviction kicks in.
    /// Defaults to 10 GB (10_737_418_240).
    /// </summary>
    public long CacheMaxBytes { get; set; } = 10_737_418_240L;  // 10 GB

    /// <summary>
    /// After eviction is triggered, cache is reduced to this fraction of
    /// <see cref="CacheMaxBytes"/> to create breathing room before the next eviction.
    /// Value must be between 0.5 and 0.95. Defaults to 0.80 (80 %).
    /// </summary>
    public double EvictionTargetRatio { get; set; } = 0.80;
}
