namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Service contract for image processing operations such as thumbnail generation.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Generates a thumbnail from the source image, constraining it to the supplied dimensions
    /// while preserving the aspect ratio. Saves the result at <paramref name="destinationPath"/>.
    /// </summary>
    /// <param name="sourcePath">Absolute path to the source image file.</param>
    /// <param name="destinationPath">Absolute path where the thumbnail should be written.</param>
    /// <param name="maxWidth">Maximum thumbnail width in pixels.</param>
    /// <param name="maxHeight">Maximum thumbnail height in pixels.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The actual pixel dimensions <c>(Width, Height)</c> of the generated thumbnail.</returns>
    Task<(int Width, int Height)> GenerateThumbnailAsync(
        string sourcePath,
        string destinationPath,
        int maxWidth,
        int maxHeight,
        CancellationToken cancellationToken = default);
}
