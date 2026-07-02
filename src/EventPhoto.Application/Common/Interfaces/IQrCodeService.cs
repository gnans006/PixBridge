namespace EventPhoto.Application.Common.Interfaces;

/// <summary>Contract for generating QR code images.</summary>
public interface IQrCodeService
{
    /// <summary>Generates a QR code PNG image encoding the given URL and saves it to destinationPath.
    /// When <paramref name="eventName"/> is provided the image includes a styled label above the QR code.</summary>
    Task GenerateAsync(
        string url,
        string destinationPath,
        string? eventName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Backward-compatible alias for <see cref="GenerateAsync(string,string,string,CancellationToken)"/>.</summary>
    async Task<string> GenerateQrCodeAsync(string content, string outputPath, CancellationToken cancellationToken = default)
    {
        await GenerateAsync(content, outputPath, null, cancellationToken);
        return Path.GetFullPath(outputPath);
    }
}
