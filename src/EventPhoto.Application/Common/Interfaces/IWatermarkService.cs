using EventPhoto.Domain.Entities;

namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Provides in-memory watermark rendering over raw image bytes.
/// Original image data is never modified; all processing occurs on a copy.
/// </summary>
public interface IWatermarkService
{
    /// <summary>
    /// Applies a watermark to <paramref name="imageBytes"/> according to
    /// <paramref name="config"/> and returns the watermarked image bytes.
    /// </summary>
    /// <param name="imageBytes">The raw bytes of the original image.</param>
    /// <param name="config">The watermark configuration to apply.</param>
    /// <param name="context">Runtime tokens resolved during template rendering.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The watermarked image bytes encoded in the same format as the original.
    /// If watermarking is disabled or skipped, the original bytes are returned
    /// without allocating a new array.
    /// </returns>
    Task<byte[]> ApplyWatermarkAsync(
        byte[] imageBytes,
        WatermarkConfiguration config,
        WatermarkContext context,
        CancellationToken cancellationToken = default);
}
