using EventPhoto.Application.Common.Interfaces;
using QRCoder;

namespace EventPhoto.Infrastructure.Services.QrCode;

/// <summary>
/// QRCoder-backed implementation of <see cref="IQrCodeService"/>.
/// Generates a PNG QR code and writes it to a file.
/// </summary>
public sealed class QrCodeService : IQrCodeService
{
    /// <inheritdoc />
    public async Task GenerateAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = qrCode.GetGraphic(pixelsPerModule: 20);

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(destinationPath, pngBytes, cancellationToken);
    }
}
