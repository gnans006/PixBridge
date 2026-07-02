using EventPhoto.Application.Common.Interfaces;
using QRCoder;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using IoPath = System.IO.Path;

namespace EventPhoto.Infrastructure.Services.QrCode;

/// <summary>
/// QRCoder-backed implementation of <see cref="IQrCodeService"/>.
/// When an event name is supplied the generated PNG includes a styled label
/// on top of the QR code for easy identification when printed or shared.
/// </summary>
public sealed class QrCodeService : IQrCodeService
{
    /// <inheritdoc />
    public async Task GenerateAsync(
        string url,
        string destinationPath,
        string? eventName = null,
        CancellationToken cancellationToken = default)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = qrCode.GetGraphic(pixelsPerModule: 20);

        var directory = IoPath.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Without an event name or fonts, just write the raw QR code.
        if (string.IsNullOrWhiteSpace(eventName) || !TryGetFont(28, FontStyle.Bold, out var titleFont))
        {
            await File.WriteAllBytesAsync(destinationPath, pngBytes, cancellationToken);
            return;
        }

        using var qrImage = Image.Load<Rgba32>(pngBytes);
        const int headerHeight = 80;
        const int footerHeight = 36;
        var width = qrImage.Width;
        var height = qrImage.Height + headerHeight + footerHeight;

        using var canvas = new Image<Rgba32>(width, height);
        canvas.Mutate(ctx =>
        {
            ctx.Fill(Color.White);

            // Teal header strip
            ctx.Fill(
                new SolidBrush(Color.ParseHex("0f766e")),
                new RectangularPolygon(0, 0, width, headerHeight));

            // Darker footer strip
            ctx.Fill(
                new SolidBrush(Color.ParseHex("134e4a")),
                new RectangularPolygon(0, height - footerHeight, width, footerHeight));

            // QR code in the white area
            ctx.DrawImage(qrImage, new Point(0, headerHeight), 1f);

            // Event name centred in the header
            var displayName = eventName.Length > 32 ? string.Concat(eventName.AsSpan(0, 29), "...") : eventName;
            ctx.DrawText(
                new RichTextOptions(titleFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Origin = new PointF(width / 2f, headerHeight / 2f),
                    WrappingLength = width - 24,
                },
                displayName,
                Color.White);

            // Footer caption
            if (TryGetFont(13, FontStyle.Regular, out var footerFont))
            {
                ctx.DrawText(
                    new RichTextOptions(footerFont)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new PointF(width / 2f, height - footerHeight / 2f),
                    },
                    "Scan to view photos",
                    Color.ParseHex("99f6e4"));
            }
        });

        await using var stream = new FileStream(
            destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await canvas.SaveAsPngAsync(stream, cancellationToken);
    }

    private static bool TryGetFont(float size, FontStyle style, out Font font)
    {
        font = default!;
        foreach (var name in new[] { "Segoe UI", "Arial", "Helvetica", "Liberation Sans", "DejaVu Sans" })
        {
            if (SystemFonts.Collection.TryGet(name, out var family))
            {
                font = family.CreateFont(size, style);
                return true;
            }
        }
        // Fall back to any installed font
        foreach (var family in SystemFonts.Collection.Families)
        {
            font = family.CreateFont(size, style);
            return true;
        }
        return false;
    }
}
