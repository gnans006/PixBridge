namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Manages a disk-based cache of pre-watermarked photo files.
///
/// <para>
/// Each cached entry is keyed by <c>(photoId, configHash)</c> where
/// <c>configHash</c> is a stable fingerprint of the active watermark settings.
/// Changing any watermark property produces a new hash, automatically
/// invalidating all previously cached files for that event without any
/// explicit bookkeeping.
/// </para>
///
/// <para>
/// The cache is a pure optimisation layer — original photo files are never
/// modified. The entire cache directory can be deleted at any time; files
/// will be regenerated on the next download request.
/// </para>
/// </summary>
public interface IWatermarkCacheService
{
    /// <summary>
    /// Returns the absolute path to a cached watermarked file, or
    /// <see langword="null"/> when no valid entry exists (cache miss).
    /// </summary>
    /// <param name="photoId">The photo identifier.</param>
    /// <param name="eventId">The event identifier (determines cache subfolder).</param>
    /// <param name="configHash">
    /// A stable hash of the current watermark configuration.
    /// See <see cref="ComputeConfigHash"/>.
    /// </param>
    string? GetCachedPath(Guid photoId, Guid eventId, string configHash);

    /// <summary>
    /// Persists watermarked <paramref name="watermarkedBytes"/> to the cache and
    /// returns the absolute path of the written file.
    /// </summary>
    /// <remarks>
    /// After writing, this method checks whether total cache size exceeds the
    /// configured limit and evicts the least-recently-accessed files if needed.
    /// </remarks>
    Task<string> SaveAsync(
        Guid photoId,
        Guid eventId,
        string configHash,
        byte[] watermarkedBytes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all cached files for the specified event.
    /// Called when watermark configuration is updated or an event is deleted.
    /// </summary>
    void InvalidateEvent(Guid eventId);

    /// <summary>
    /// Deletes any cached files for a single photo across all config hashes.
    /// Called when a photo is deleted.
    /// </summary>
    void InvalidatePhoto(Guid photoId, Guid eventId);

    /// <summary>
    /// Removes all cached files across all events.
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Returns aggregate statistics about the current cache state.
    /// </summary>
    WatermarkCacheStats GetStats();

    /// <summary>
    /// Returns per-event statistics useful for the management UI.
    /// </summary>
    IReadOnlyList<WatermarkCacheEventStats> GetEventStats();

    /// <summary>
    /// Computes a stable, compact hash string that uniquely identifies
    /// a watermark configuration snapshot. Used as part of the cache key.
    /// </summary>
    string ComputeConfigHash(
        string mode,
        string style,
        float opacity,
        string scale,
        string? customText,
        string? template,
        bool includeStudioName,
        bool includeEventName,
        bool includeDownloadDate,
        string textColor,
        string? fontName,
        float backgroundOpacity);
}

/// <summary>Aggregate cache statistics.</summary>
/// <param name="TotalSizeBytes">Total bytes occupied by all cached files.</param>
/// <param name="TotalFileCount">Total number of cached files.</param>
/// <param name="MaxSizeBytes">Configured maximum cache size in bytes.</param>
/// <param name="CacheDirectory">Absolute path to the cache root folder.</param>
public sealed record WatermarkCacheStats(
    long TotalSizeBytes,
    int TotalFileCount,
    long MaxSizeBytes,
    string CacheDirectory);

/// <summary>Per-event cache statistics.</summary>
/// <param name="EventId">The event identifier.</param>
/// <param name="SizeBytes">Bytes used by this event's cached files.</param>
/// <param name="FileCount">Number of cached files for this event.</param>
public sealed record WatermarkCacheEventStats(
    Guid EventId,
    long SizeBytes,
    int FileCount);
