namespace EventPhoto.Application.Common.Interfaces;

/// <summary>Contract for file system operations on photo storage.</summary>
public interface IFileStorageService
{
    /// <summary>Returns the URL path to serve a photo original.</summary>
    string GetPhotoUrl(string originalPath, Guid eventId);

    /// <summary>Returns the URL path to serve a thumbnail.</summary>
    string GetThumbnailUrl(string thumbnailPath, Guid eventId);

    /// <summary>Checks whether a file exists on disk.</summary>
    bool FileExists(string absolutePath);

    /// <summary>Returns file size in bytes, or -1 if file not found.</summary>
    long GetFileSize(string absolutePath);

    /// <summary>Creates a directory if it does not already exist.</summary>
    void EnsureDirectoryExists(string directoryPath);

    /// <summary>Streams a file for download.</summary>
    Stream OpenRead(string absolutePath);

    /// <summary>Backward-compatible helper that reads all bytes from a file.</summary>
    async Task<byte[]> ReadAllBytesAsync(string absolutePath, CancellationToken cancellationToken = default)
    {
        using var stream = OpenRead(absolutePath);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }

    /// <summary>Backward-compatible helper that deletes a file if it exists.</summary>
    Task DeleteFileAsync(string absolutePath, CancellationToken cancellationToken = default)
    {
        if (FileExists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>Backward-compatible helper that ensures a directory exists.</summary>
    Task EnsureDirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(directoryPath);
        return Task.CompletedTask;
    }
}
