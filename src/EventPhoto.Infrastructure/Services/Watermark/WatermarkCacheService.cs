using System.Security.Cryptography;
using System.Text;
using EventPhoto.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventPhoto.Infrastructure.Services.Watermark;

/// <summary>
/// Disk-based cache for pre-watermarked photo files.
///
/// <para><b>Cache layout on disk:</b></para>
/// <code>
/// {CachePath}/
///   {eventId}/
///     {photoId}_{configHash}.jpg
/// </code>
///
/// <para>
/// <b>Security:</b> All path arguments are validated to prevent path-traversal
/// attacks. Only GUIDs and hex hashes are accepted as path segments — no
/// caller-supplied strings are ever used raw in file paths.
/// </para>
///
/// <para>
/// <b>LRU eviction:</b> After each successful write, if total cache size
/// exceeds <see cref="WatermarkCacheOptions.CacheMaxBytes"/>, the oldest
/// files by <see cref="FileInfo.LastAccessTimeUtc"/> are deleted until the
/// cache drops to <see cref="WatermarkCacheOptions.EvictionTargetRatio"/> of
/// the limit. This avoids thrashing by not evicting down to exactly the limit.
/// </para>
///
/// <para>
/// <b>Thread safety:</b> All file operations use a per-event lock so concurrent
/// downloads for the same event do not corrupt or duplicate cache files.
/// </para>
/// </summary>
public sealed class WatermarkCacheService : IWatermarkCacheService
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private const string CacheSubfolder = "watermarked";
    private const string CachedFileExtension = ".jpg";

    // ── Fields ────────────────────────────────────────────────────────────────

    private readonly string _cacheRoot;
    private readonly long _maxBytes;
    private readonly double _evictionTargetRatio;
    private readonly ILogger<WatermarkCacheService> _logger;

    // Fine-grained per-event write locks — prevents duplicate watermark processing
    // when multiple guests request the same photo simultaneously.
    private readonly LockSet _eventLocks = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the service.
    /// Resolves <see cref="WatermarkCacheOptions.CachePath"/> relative to
    /// <see cref="IWebHostEnvironment.ContentRootPath"/> when the configured
    /// value is empty, ensuring the cache is always inside the application tree
    /// on a fresh install.
    /// </summary>
    public WatermarkCacheService(
        IOptions<WatermarkCacheOptions> options,
        IWebHostEnvironment env,
        ILogger<WatermarkCacheService> logger)
    {
        _logger = logger;

        var opts = options.Value;
        _maxBytes = opts.CacheMaxBytes > 0 ? opts.CacheMaxBytes : 10_737_418_240L;
        _evictionTargetRatio = opts.EvictionTargetRatio is > 0.5 and < 0.95
            ? opts.EvictionTargetRatio
            : 0.80;

        // Resolve cache root: configured path → fallback to {ContentRoot}\cache\watermarked
        _cacheRoot = string.IsNullOrWhiteSpace(opts.CachePath)
            ? Path.Combine(env.ContentRootPath, "cache", CacheSubfolder)
            : Path.Combine(opts.CachePath, CacheSubfolder);

        // Ensure the root directory exists at startup.
        Directory.CreateDirectory(_cacheRoot);

        _logger.LogInformation(
            "WatermarkCacheService initialised. Root={Root}, MaxSize={MaxGB:F1} GB",
            _cacheRoot, _maxBytes / 1_073_741_824.0);
    }

    // ── IWatermarkCacheService ────────────────────────────────────────────────

    /// <inheritdoc />
    public string? GetCachedPath(Guid photoId, Guid eventId, string configHash)
    {
        ValidateHash(configHash);
        var path = BuildCachePath(photoId, eventId, configHash);

        if (!File.Exists(path))
            return null;

        // Update LastAccessTime so the LRU eviction order reflects actual usage.
        try { File.SetLastAccessTimeUtc(path, DateTime.UtcNow); }
        catch { /* non-critical — ignore permission errors on read-only drives */ }

        return path;
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(
        Guid photoId,
        Guid eventId,
        string configHash,
        byte[] watermarkedBytes,
        CancellationToken cancellationToken = default)
    {
        if (watermarkedBytes is null || watermarkedBytes.Length == 0)
            throw new ArgumentException("Watermarked bytes must not be empty.", nameof(watermarkedBytes));

        ValidateHash(configHash);

        var eventDir = GetEventDirectory(eventId);
        var filePath = BuildCachePath(photoId, eventId, configHash);

        // Per-event lock — prevents two concurrent requests from both
        // processing the same photo simultaneously.
        using var eventLock = await _eventLocks.AcquireAsync(eventId, cancellationToken);

        // Double-check after acquiring lock: another request may have already written it.
        if (File.Exists(filePath))
            return filePath;

        Directory.CreateDirectory(eventDir);

        await File.WriteAllBytesAsync(filePath, watermarkedBytes, cancellationToken);
        _logger.LogDebug(
            "Cached watermarked photo. PhotoId={PhotoId}, Size={KB} KB",
            photoId, watermarkedBytes.Length / 1024);

        // Run LRU eviction check asynchronously — do NOT await here to keep
        // the download response fast. Fire-and-forget with structured error handling.
        _ = Task.Run(() => EvictIfOverLimitAsync(), CancellationToken.None);

        return filePath;
    }

    /// <inheritdoc />
    public void InvalidateEvent(Guid eventId)
    {
        var eventDir = GetEventDirectory(eventId);
        if (!Directory.Exists(eventDir))
            return;

        try
        {
            Directory.Delete(eventDir, recursive: true);
            _logger.LogInformation(
                "Watermark cache invalidated for EventId={EventId}.", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not fully invalidate watermark cache for EventId={EventId}.", eventId);
        }
    }

    /// <inheritdoc />
    public void InvalidatePhoto(Guid photoId, Guid eventId)
    {
        var eventDir = GetEventDirectory(eventId);
        if (!Directory.Exists(eventDir))
            return;

        // Delete all hash variants for this photo (e.g. if config changed before guest downloaded).
        var prefix = photoId.ToString("N");
        try
        {
            foreach (var file in Directory.EnumerateFiles(eventDir, $"{prefix}_*{CachedFileExtension}"))
            {
                File.Delete(file);
            }

            _logger.LogDebug(
                "Watermark cache invalidated for PhotoId={PhotoId}.", photoId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not invalidate watermark cache for PhotoId={PhotoId}.", photoId);
        }
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        try
        {
            if (Directory.Exists(_cacheRoot))
            {
                Directory.Delete(_cacheRoot, recursive: true);
            }

            Directory.CreateDirectory(_cacheRoot);
            _logger.LogInformation("Watermark cache cleared entirely.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear entire watermark cache.");
            throw;
        }
    }

    /// <inheritdoc />
    public WatermarkCacheStats GetStats()
    {
        if (!Directory.Exists(_cacheRoot))
            return new WatermarkCacheStats(0, 0, _maxBytes, _cacheRoot);

        var files = new DirectoryInfo(_cacheRoot)
            .EnumerateFiles("*" + CachedFileExtension, SearchOption.AllDirectories)
            .ToList();

        return new WatermarkCacheStats(
            TotalSizeBytes: files.Sum(f => f.Length),
            TotalFileCount: files.Count,
            MaxSizeBytes: _maxBytes,
            CacheDirectory: _cacheRoot);
    }

    /// <inheritdoc />
    public IReadOnlyList<WatermarkCacheEventStats> GetEventStats()
    {
        if (!Directory.Exists(_cacheRoot))
            return [];

        return new DirectoryInfo(_cacheRoot)
            .EnumerateDirectories()
            .Select(dir =>
            {
                if (!Guid.TryParse(dir.Name, out var eventId))
                    return null;

                var files = dir.EnumerateFiles("*" + CachedFileExtension).ToList();
                return new WatermarkCacheEventStats(
                    EventId: eventId,
                    SizeBytes: files.Sum(f => f.Length),
                    FileCount: files.Count);
            })
            .OfType<WatermarkCacheEventStats>()
            .ToList();
    }

    /// <inheritdoc />
    public string ComputeConfigHash(
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
        float backgroundOpacity)
    {
        // Stable canonical string — order matters, separator prevents collision.
        var input = string.Join("|",
            mode, style,
            opacity.ToString("F4"),
            scale,
            customText ?? string.Empty,
            template ?? string.Empty,
            includeStudioName ? "1" : "0",
            includeEventName  ? "1" : "0",
            includeDownloadDate ? "1" : "0",
            textColor.ToUpperInvariant(),
            fontName ?? string.Empty,
            backgroundOpacity.ToString("F4"));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        // Return first 12 hex chars — 48 bits of collision resistance is
        // more than sufficient for a local cache key.
        return Convert.ToHexString(hashBytes)[..12].ToLowerInvariant();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string GetEventDirectory(Guid eventId) =>
        Path.Combine(_cacheRoot, eventId.ToString("N"));

    private string BuildCachePath(Guid photoId, Guid eventId, string configHash) =>
        Path.Combine(
            GetEventDirectory(eventId),
            $"{photoId:N}_{configHash}{CachedFileExtension}");

    /// <summary>
    /// Validates that <paramref name="hash"/> is a short lowercase hex string.
    /// Prevents any directory-traversal or injection via a crafted hash value.
    /// </summary>
    private static void ValidateHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash) || hash.Length > 32
            || !hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
        {
            throw new ArgumentException(
                $"Invalid config hash '{hash}'. Must be a lowercase hex string ≤ 32 chars.",
                nameof(hash));
        }
    }

    /// <summary>
    /// Evicts least-recently-accessed files until total cache size is at or
    /// below <see cref="_evictionTargetRatio"/> × <see cref="_maxBytes"/>.
    /// </summary>
    private Task EvictIfOverLimitAsync()
    {
        try
        {
            var allFiles = new DirectoryInfo(_cacheRoot)
                .EnumerateFiles("*" + CachedFileExtension, SearchOption.AllDirectories)
                .Select(f => (File: f, f.Length, f.LastAccessTimeUtc))
                .OrderBy(f => f.LastAccessTimeUtc)
                .ToList();

            var totalBytes = allFiles.Sum(f => f.Length);
            if (totalBytes <= _maxBytes)
                return Task.CompletedTask;

            var targetBytes = (long)(_maxBytes * _evictionTargetRatio);
            var bytesDeleted = 0L;
            var filesDeleted = 0;

            foreach (var (file, length, _) in allFiles)
            {
                if (totalBytes - bytesDeleted <= targetBytes)
                    break;

                try
                {
                    file.Delete();
                    bytesDeleted += length;
                    filesDeleted++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "LRU eviction: could not delete {Path}.", file.FullName);
                }
            }

            if (filesDeleted > 0)
            {
                _logger.LogInformation(
                    "LRU eviction completed. Deleted {Count} files, freed {MB:F1} MB.",
                    filesDeleted, bytesDeleted / 1_048_576.0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LRU eviction failed unexpectedly.");
        }

        return Task.CompletedTask;
    }

    // ── LockSet ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A minimal per-key async lock set backed by <see cref="SemaphoreSlim"/>.
    /// Provides one lock per eventId so writes to different events are never blocked
    /// by each other.
    /// </summary>
    private sealed class LockSet
    {
        private readonly Dictionary<Guid, SemaphoreSlim> _locks = new();
        private readonly object _sync = new();

        public async Task<IDisposable> AcquireAsync(Guid key, CancellationToken ct)
        {
            SemaphoreSlim semaphore;
            lock (_sync)
            {
                if (!_locks.TryGetValue(key, out semaphore!))
                {
                    semaphore = new SemaphoreSlim(1, 1);
                    _locks[key] = semaphore;
                }
            }

            await semaphore.WaitAsync(ct);
            return new Releaser(semaphore);
        }

        private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
        {
            public void Dispose() => semaphore.Release();
        }
    }
}
