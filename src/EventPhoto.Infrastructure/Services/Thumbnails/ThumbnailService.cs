using EventPhoto.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace EventPhoto.Infrastructure.Services.Thumbnails;

/// <summary>
/// ImageSharp-backed implementation of <see cref="IThumbnailService"/> for thumbnail generation.
/// </summary>
public sealed class ThumbnailService : IThumbnailService, IImageService
{
    /// <inheritdoc />
    public async Task<(int Width, int Height)> GenerateAsync(
        string sourcePath,
        string destinationPath,
        int maxWidth,
        int maxHeight,
        int quality,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        using var image = await Image.LoadAsync(sourcePath, cancellationToken);
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxWidth, maxHeight)
        }));

        await image.SaveAsync(destinationPath, new JpegEncoder { Quality = quality }, cancellationToken);
        return (image.Width, image.Height);
    }

    /// <inheritdoc />
    public Task<(int Width, int Height)> GenerateThumbnailAsync(
        string sourcePath,
        string destinationPath,
        int maxWidth,
        int maxHeight,
        CancellationToken cancellationToken = default) =>
        GenerateAsync(sourcePath, destinationPath, maxWidth, maxHeight, 85, cancellationToken);
}
