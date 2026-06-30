namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Service contract for file-system operations used by application handlers.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Reads all bytes from the file at the specified path.
    /// </summary>
    /// <param name="path">Absolute file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw file content.</returns>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> if the file exists on disk.
    /// </summary>
    /// <param name="path">Absolute file path.</param>
    bool FileExists(string path);

    /// <summary>
    /// Deletes the file at the specified path if it exists.
    /// </summary>
    /// <param name="path">Absolute file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures that the directory at <paramref name="path"/> exists, creating it if necessary.
    /// </summary>
    /// <param name="path">Absolute directory path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task EnsureDirectoryExistsAsync(string path, CancellationToken cancellationToken = default);
}
