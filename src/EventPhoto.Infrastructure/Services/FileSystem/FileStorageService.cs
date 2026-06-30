using EventPhoto.Application.Common.Interfaces;

namespace EventPhoto.Infrastructure.Services.FileSystem;

/// <summary>Local file system implementation of <see cref="IFileStorageService"/>.</summary>
public sealed class FileStorageService : IFileStorageService
{
    /// <inheritdoc />
    public string GetPhotoUrl(string originalPath, Guid eventId) =>
        $"/api/photos/{eventId}/files/{Uri.EscapeDataString(Path.GetFileName(originalPath))}";

    /// <inheritdoc />
    public string GetThumbnailUrl(string thumbnailPath, Guid eventId) =>
        $"/api/photos/{eventId}/thumbnails/{Uri.EscapeDataString(Path.GetFileName(thumbnailPath))}";

    /// <inheritdoc />
    public bool FileExists(string absolutePath) => File.Exists(absolutePath);

    /// <inheritdoc />
    public long GetFileSize(string absolutePath)
    {
        var info = new FileInfo(absolutePath);
        return info.Exists ? info.Length : -1;
    }

    /// <inheritdoc />
    public void EnsureDirectoryExists(string directoryPath) => Directory.CreateDirectory(directoryPath);

    /// <inheritdoc />
    public Stream OpenRead(string absolutePath) =>
        new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
}
