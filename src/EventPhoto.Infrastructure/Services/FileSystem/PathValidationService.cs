using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Infrastructure.Services.FileSystem;

/// <summary>
/// Server-side path validation for watch folders.
///
/// Design goals:
/// - ≤ 20 ms on local/fixed drives (typical: 1–5 ms)
/// - Safe on removable drives (USB, SD card) — reports DriveType, does not block
/// - Detects missing drives (E: not connected) immediately
/// - Checks write permission without leaving temp files behind
/// - Never throws — always returns a structured result
/// </summary>
public sealed class PathValidationService : IPathValidationService
{
    // Windows reserved device names — these are invalid as path components on any Windows version.
    private static readonly HashSet<string> WindowsReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private readonly IEventRepository _eventRepository;
    private readonly ILogger<PathValidationService> _logger;

    public PathValidationService(IEventRepository eventRepository, ILogger<PathValidationService> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PathValidationResult> ValidateAsync(
        string path,
        Guid? excludeEventId = null,
        CancellationToken ct = default)
    {
        // ── 1. Null / empty ───────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(path))
            return Fail("Watch folder path is required.");

        path = path.Trim();

        // ── 2. Length ─────────────────────────────────────────────────────────
        if (path.Length < 3)
            return Fail("Watch folder path is too short.");

        if (path.Length > 512)
            return Fail("Watch folder path must not exceed 512 characters.");

        // ── 3. Path traversal ─────────────────────────────────────────────────
        if (path.Contains(".."))
            return Fail("Path must not contain path traversal sequences (..).");

        // ── 4. Absolute path required ─────────────────────────────────────────
        // Relative paths would resolve to the API's working directory — dangerous.
        if (!Path.IsPathRooted(path) || !Path.IsPathFullyQualified(path))
            return Fail("Watch folder must be an absolute path (e.g. D:\\Events\\Wedding_2026).");

        // ── 5. Invalid path characters ────────────────────────────────────────
        var invalidChars = Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c)))
            return Fail("Watch folder path contains invalid characters.");

        // ── 6. Windows reserved names in any path segment ────────────────────
        var segments = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            // Strip extension so e.g. "CON.txt" is still caught
            var name = Path.GetFileNameWithoutExtension(segment);
            if (WindowsReservedNames.Contains(name))
                return Fail($"'{segment}' is a reserved system name and cannot be used as a folder name.");
        }

        // ── 7. Drive-level checks (Windows only) ─────────────────────────────
        string? driveType = null;
        string? driveLabel = null;
        string? warning = null;

        if (OperatingSystem.IsWindows())
        {
            var root = Path.GetPathRoot(path);
            if (!string.IsNullOrEmpty(root))
            {
                // Check if drive letter exists at all
                var allDrives = DriveInfo.GetDrives();
                var drive = allDrives.FirstOrDefault(d =>
                    string.Equals(d.RootDirectory.FullName, root, StringComparison.OrdinalIgnoreCase));

                if (drive is null)
                    return Fail($"Drive '{root.TrimEnd(Path.DirectorySeparatorChar)}' does not exist or is not connected. " +
                                "Please plug in the drive and try again.");

                // Classify drive type
                driveType = drive.DriveType switch
                {
                    DriveType.Removable => "Removable",
                    DriveType.Fixed     => "Fixed",
                    DriveType.Network   => "Network",
                    DriveType.CDRom     => "CDRom",
                    DriveType.Ram       => "Ram",
                    _                   => "Unknown"
                };

                // Drive ready check (catches drives that exist but aren't spun up / ejected)
                if (!drive.IsReady)
                    return Fail($"Drive '{root.TrimEnd(Path.DirectorySeparatorChar)}' is not ready. " +
                                "It may be ejecting, formatting, or not mounted.");

                driveLabel = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? $"{root.TrimEnd(Path.DirectorySeparatorChar)} Drive"
                    : drive.VolumeLabel;

                // Warn about removable / network drives
                warning = driveType switch
                {
                    "Removable" =>
                        $"This path is on a removable drive ({root.TrimEnd(Path.DirectorySeparatorChar)}). " +
                        "If the drive is disconnected, PixBridge will stop watching for new photos until it is reconnected.",
                    "Network" =>
                        "This path is on a network drive. Network drives may be slower and can disconnect unexpectedly.",
                    "CDRom" =>
                        "CD-ROM drives are not suitable for watching — they are read-only.",
                    _ => null
                };

                // Block CD-ROM
                if (driveType == "CDRom")
                    return Fail("CD-ROM drives cannot be used as watch folders (read-only).");
            }
        }

        // ── 8. Does the directory already exist? ─────────────────────────────
        bool exists = Directory.Exists(path);

        // ── 9. Write-permission probe ─────────────────────────────────────────
        // We need to confirm PixBridge can actually write here.
        // Strategy: if directory exists → probe directly; if not → probe the
        // nearest existing ancestor (the parent that will be created).
        string probeDir = path;
        if (!exists)
        {
            // Walk up until we find an existing directory
            var ancestor = new DirectoryInfo(path);
            while (ancestor.Parent != null && !ancestor.Parent.Exists)
                ancestor = ancestor.Parent;

            if (ancestor.Parent != null && ancestor.Parent.Exists)
                probeDir = ancestor.Parent.FullName;
            else
                probeDir = Path.GetPathRoot(path) ?? path;
        }

        if (!string.IsNullOrEmpty(probeDir) && Directory.Exists(probeDir))
        {
            var tempFile = Path.Combine(probeDir, $".pixbridge_probe_{Guid.NewGuid():N}");
            try
            {
                await File.WriteAllTextAsync(tempFile, "probe", ct);
                File.Delete(tempFile);
            }
            catch (UnauthorizedAccessException)
            {
                return Fail("PixBridge does not have permission to write to this location. " +
                            "Run PixBridge as Administrator or choose a different folder.");
            }
            catch (IOException ex)
            {
                return Fail($"Cannot write to this location: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                return Fail("Validation was cancelled.");
            }
        }

        // ── 10. Duplicate path check (another event already uses this folder) ─
        var folders = await _eventRepository.GetWatchFoldersAsync(ct);
        var conflict = folders
            .Where(f => excludeEventId == null || f.Id != excludeEventId)
            .FirstOrDefault(f => string.Equals(f.WatchFolder, path, StringComparison.OrdinalIgnoreCase));

        if (conflict != default)
            return Fail($"This folder is already used by event '{conflict.Name}'. Each event must have its own folder.");

        // ── All checks passed ─────────────────────────────────────────────────
        return new PathValidationResult(
            IsValid: true,
            Exists: exists,
            WillBeCreated: !exists,
            DriveType: driveType,
            DriveLabel: driveLabel,
            Warning: warning,
            Error: null);
    }

    /// <inheritdoc />
    public IReadOnlyList<DriveInfoResult> GetAvailableDrives()
    {
        if (!OperatingSystem.IsWindows())
            return Array.Empty<DriveInfoResult>();

        try
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType != DriveType.CDRom)
                .Select(d => new DriveInfoResult(
                    Letter: d.RootDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar),
                    Label: string.IsNullOrWhiteSpace(d.VolumeLabel)
                        ? $"{d.RootDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar)} Drive"
                        : d.VolumeLabel,
                    Type: d.DriveType switch
                    {
                        DriveType.Removable => "Removable",
                        DriveType.Fixed     => "Fixed",
                        DriveType.Network   => "Network",
                        DriveType.Ram       => "Ram",
                        _                   => "Unknown"
                    },
                    TotalBytes: d.TotalSize,
                    FreeBytes: d.AvailableFreeSpace))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate drives.");
            return Array.Empty<DriveInfoResult>();
        }
    }

    private static PathValidationResult Fail(string error) =>
        new(IsValid: false, Exists: false, WillBeCreated: false,
            DriveType: null, DriveLabel: null, Warning: null, Error: error);
}
