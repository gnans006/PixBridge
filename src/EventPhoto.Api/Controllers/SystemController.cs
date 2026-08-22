using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// System utility endpoints — path validation, drive discovery, watermark cache management.
/// </summary>
[ApiController]
[Route("api/system")]
[Authorize(Policy = "OperatorOrAbove")]
[Produces("application/json")]
public sealed class SystemController : ControllerBase
{
    private readonly IPathValidationService _pathValidation;
    private readonly IWatermarkCacheService _watermarkCache;

    /// <summary>Initializes a new instance of <see cref="SystemController"/>.</summary>
    public SystemController(
        IPathValidationService pathValidation,
        IWatermarkCacheService watermarkCache)
    {
        _pathValidation = pathValidation;
        _watermarkCache = watermarkCache;
    }

    /// <summary>
    /// Validates a candidate watch-folder path.
    /// Checks format, drive existence, drive type, write permission and duplicate usage.
    /// Designed to be called as the user types (debounced) — fast ≤ 20 ms on local drives.
    /// Does NOT create any folders.
    /// </summary>
    [HttpPost("validate-path")]
    [ProducesResponseType(typeof(ApiResponse<PathValidationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidatePath(
        [FromBody] ValidatePathRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _pathValidation.ValidateAsync(
            request.Path ?? string.Empty,
            request.ExcludeEventId,
            cancellationToken);

        var response = new PathValidationResponse(
            result.IsValid,
            result.Exists,
            result.WillBeCreated,
            result.DriveType,
            result.DriveLabel,
            result.Warning,
            result.Error);

        return Ok(ApiResponse<PathValidationResponse>.Ok(response));
    }

    /// <summary>
    /// Returns all currently available drives.
    /// Used by the watch-folder browser helper in the UI.
    /// </summary>
    [HttpGet("drives")]
    [ProducesResponseType(typeof(ApiResponse<List<DriveResponse>>), StatusCodes.Status200OK)]
    public IActionResult GetDrives()
    {
        var drives = _pathValidation.GetAvailableDrives();
        var response = drives.Select(d => new DriveResponse(
            d.Letter,
            d.Label,
            d.Type,
            d.TotalBytes,
            d.FreeBytes,
            FriendlySize(d.FreeBytes))).ToList();

        return Ok(ApiResponse<List<DriveResponse>>.Ok(response));
    }

    // ── Watermark Cache Management ────────────────────────────────────────────
    // Restricted to StudioOwner only — destructive operations must not be
    // accessible to Operators or Managers.

    /// <summary>
    /// Returns aggregate and per-event watermark cache statistics.
    /// </summary>
    [HttpGet("cache/stats")]
    [Authorize(Policy = "OwnerOnly")]
    [ProducesResponseType(typeof(ApiResponse<CacheStatsResponse>), StatusCodes.Status200OK)]
    public IActionResult GetCacheStats()
    {
        var stats      = _watermarkCache.GetStats();
        var eventStats = _watermarkCache.GetEventStats();

        var response = new CacheStatsResponse(
            TotalSizeBytes:   stats.TotalSizeBytes,
            TotalFileCount:   stats.TotalFileCount,
            MaxSizeBytes:     stats.MaxSizeBytes,
            TotalSizeFormatted: FriendlySize(stats.TotalSizeBytes),
            MaxSizeFormatted:   FriendlySize(stats.MaxSizeBytes),
            CacheDirectory:   stats.CacheDirectory,
            Events: eventStats.Select(e => new CacheEventStatsResponse(
                e.EventId,
                e.SizeBytes,
                FriendlySize(e.SizeBytes),
                e.FileCount)).ToList());

        return Ok(ApiResponse<CacheStatsResponse>.Ok(response));
    }

    /// <summary>
    /// Clears the watermark cache for a single event.
    /// Safe to call at any time — the cache regenerates on the next download.
    /// </summary>
    [HttpDelete("cache/event/{eventId:guid}")]
    [Authorize(Policy = "OwnerOnly")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult ClearEventCache(Guid eventId)
    {
        _watermarkCache.InvalidateEvent(eventId);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Clears the entire watermark cache across all events.
    /// ⚠ This is a destructive operation — all pre-processed files are deleted.
    /// Files are regenerated on next download; guests may experience slower
    /// first-download performance until the cache is warm again.
    /// </summary>
    [HttpDelete("cache")]
    [Authorize(Policy = "OwnerOnly")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult ClearAllCache()
    {
        try
        {
            _watermarkCache.InvalidateAll();
            return Ok(ApiResponse.Ok());
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail("Cache could not be cleared. Check server logs for details."));
        }
    }

    private static string FriendlySize(long bytes) => bytes switch
    {
        >= 1_099_511_627_776 => $"{bytes / 1_099_511_627_776.0:F1} TB",
        >= 1_073_741_824     => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576         => $"{bytes / 1_048_576.0:F0} MB",
        _                    => $"{bytes / 1024.0:F0} KB"
    };
}

/// <summary>Request body for path validation.</summary>
public sealed record ValidatePathRequest(
    string? Path,
    /// <summary>When editing an existing event, pass its ID to skip the self-conflict check.</summary>
    Guid? ExcludeEventId);

/// <summary>Path validation result returned to the UI.</summary>
public sealed record PathValidationResponse(
    bool IsValid,
    bool Exists,
    bool WillBeCreated,
    string? DriveType,
    string? DriveLabel,
    string? Warning,
    string? Error);

/// <summary>Drive information for the browse helper.</summary>
public sealed record DriveResponse(
    string Letter,
    string Label,
    string Type,
    long TotalBytes,
    long FreeBytes,
    string FreeFormatted);

// ── Cache response contracts ──────────────────────────────────────────────────

/// <summary>Aggregate watermark cache statistics.</summary>
public sealed record CacheStatsResponse(
    long TotalSizeBytes,
    int TotalFileCount,
    long MaxSizeBytes,
    string TotalSizeFormatted,
    string MaxSizeFormatted,
    string CacheDirectory,
    List<CacheEventStatsResponse> Events);

/// <summary>Per-event watermark cache statistics.</summary>
public sealed record CacheEventStatsResponse(
    Guid EventId,
    long SizeBytes,
    string SizeFormatted,
    int FileCount);
