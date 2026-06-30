namespace EventPhoto.Application.Common.Interfaces;

/// <summary>Contract for generating QR code images.</summary>
public interface IQrCodeService
{
    /// <summary>Generates a QR code PNG image encoding the given URL and saves it to destinationPath.</summary>
    Task GenerateAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken = default);

    /// <summary>Backward-compatible alias for <see cref="GenerateAsync(string,string,CancellationToken)"/>.</summary>
    async Task<string> GenerateQrCodeAsync(string content, string outputPath, CancellationToken cancellationToken = default)
    {
        await GenerateAsync(content, outputPath, cancellationToken);
        return Path.GetFullPath(outputPath);
    }
}
