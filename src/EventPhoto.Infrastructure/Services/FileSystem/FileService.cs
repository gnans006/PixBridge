using EventPhoto.Application.Common.Interfaces;

namespace EventPhoto.Infrastructure.Services.FileSystem;

/// <summary>
/// File system implementation of <see cref="IFileService"/>.
/// </summary>
public sealed class FileService : IFileService
{
    /// <inheritdoc />
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllBytesAsync(path, cancellationToken);

    /// <inheritdoc />
    public bool FileExists(string path)
        => File.Exists(path);

    /// <inheritdoc />
    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EnsureDirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }
}
