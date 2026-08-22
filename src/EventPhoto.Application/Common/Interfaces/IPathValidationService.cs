namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Result of a server-side path validation check.
/// </summary>
public sealed record PathValidationResult(
    bool IsValid,
    bool Exists,
    bool WillBeCreated,
    string? DriveType,
    string? DriveLabel,
    string? Warning,
    string? Error);

/// <summary>
/// Validates watch folder paths before an event is created or updated.
/// Checks format, drive availability, write permissions, and path conflicts.
/// </summary>
public interface IPathValidationService
{
    /// <summary>
    /// Validates a candidate watch folder path.
    /// Fast (≤20 ms on local drives). Does NOT create any folders.
    /// </summary>
    Task<PathValidationResult> ValidateAsync(string path, Guid? excludeEventId = null, CancellationToken ct = default);

    /// <summary>
    /// Returns all currently available drives with type and label.
    /// </summary>
    IReadOnlyList<DriveInfoResult> GetAvailableDrives();
}

/// <summary>Drive information returned to the UI for the browse helper.</summary>
public sealed record DriveInfoResult(
    string Letter,
    string Label,
    string Type,
    long TotalBytes,
    long FreeBytes);
