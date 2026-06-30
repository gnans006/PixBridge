namespace EventPhoto.Application.Common.Interfaces;

/// <summary>Contract for generating photo thumbnails.</summary>
public interface IThumbnailService
{
    /// <summary>Generates a thumbnail for the given source image, returns (width, height).</summary>
    Task<(int Width, int Height)> GenerateAsync(
        string sourcePath,
        string destinationPath,
        int maxWidth,
        int maxHeight,
        int quality,
        CancellationToken cancellationToken = default);

    /// <summary>Backward-compatible helper for callers that omit quality.</summary>
    Task<(int Width, int Height)> GenerateThumbnailAsync(
        string sourcePath,
        string destinationPath,
        int maxWidth,
        int maxHeight,
        CancellationToken cancellationToken = default) =>
        GenerateAsync(sourcePath, destinationPath, maxWidth, maxHeight, 85, cancellationToken);
}
