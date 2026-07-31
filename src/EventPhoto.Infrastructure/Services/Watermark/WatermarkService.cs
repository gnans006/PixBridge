using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace EventPhoto.Infrastructure.Services.Watermark;

/// <summary>
/// Renders watermarks in memory using SixLabors.ImageSharp.
/// Original image bytes are never written to disk or modified.
/// </summary>
public sealed class WatermarkService(ILogger<WatermarkService> logger) : IWatermarkService
{
    // Scale percentages of Math.Min(width, height)
    private const float SmallFontPercent = 0.030f;
    private const float MediumFontPercent = 0.050f;
    private const float LargeFontPercent = 0.080f;
    private const float MarginPercent = 0.020f;
    private const float LogoWidthPercent = 0.15f;

    /// <inheritdoc />
    public async Task<byte[]> ApplyWatermarkAsync(
        byte[] imageBytes,
        WatermarkConfiguration config,
        WatermarkContext context,
        CancellationToken cancellationToken = default)
    {
        if (!config.Enabled || !config.ApplyOnDownload || config.Mode == WatermarkMode.Disabled)
        {
            return imageBytes;
        }

        try
        {
            var format = Image.DetectFormat(imageBytes);
            using var image = Image.Load<Rgba32>(imageBytes);

            var text = BuildWatermarkText(config, context);

            if (!string.IsNullOrWhiteSpace(text))
            {
                var fontSize  = CalculateFontSize(image.Width, image.Height, config.Scale);
                var mainFont  = ResolveFont(fontSize, config.FontName);
                var textColor = BuildColor(config.TextColor, config.Opacity);
                var margin    = (int)(Math.Min(image.Width, image.Height) * MarginPercent);

                if (config.Style == WatermarkStyle.BottomRibbon)
                {
                    // Pre-load logo outside Mutate (which is synchronous).
                    var bgColor  = BuildBgColor(config.BackgroundOpacity);
                    var subFont  = ResolveFont(fontSize * 0.65f, config.FontName);
                    var isBranding = config.Mode is WatermarkMode.StudioBranding
                                                  or WatermarkMode.EventBranding
                                                  or WatermarkMode.StudioAndEvent;

                    Image<Rgba32>? logo = null;
                    if (isBranding
                        && !string.IsNullOrWhiteSpace(config.LogoPath)
                        && File.Exists(config.LogoPath))
                    {
                        logo = await Image.LoadAsync<Rgba32>(config.LogoPath!, cancellationToken);
                    }

                    try
                    {
                        image.Mutate(ctx => RenderBottomRibbon(
                            ctx, text, mainFont, subFont, textColor, bgColor,
                            image.Width, image.Height, margin, logo));
                    }
                    finally
                    {
                        logo?.Dispose();
                    }
                }
                else
                {
                    image.Mutate(ctx => ApplyText(
                        ctx, text, mainFont, textColor,
                        config.Style, image.Width, image.Height, margin));

                    // Logo overlay for non-ribbon styles (original behaviour).
                    if (config.Mode is WatermarkMode.StudioBranding
                                       or WatermarkMode.EventBranding
                                       or WatermarkMode.StudioAndEvent
                        && !string.IsNullOrWhiteSpace(config.LogoPath)
                        && File.Exists(config.LogoPath))
                    {
                        await OverlayLogoAsync(image, config, cancellationToken);
                    }
                }
            }

            using var ms = new MemoryStream();
            var formatName = format?.Name ?? string.Empty;

            if (formatName.Equals("PNG", StringComparison.OrdinalIgnoreCase))
            {
                await image.SaveAsPngAsync(ms, new PngEncoder(), cancellationToken);
            }
            else if (formatName.Equals("WEBP", StringComparison.OrdinalIgnoreCase))
            {
                await image.SaveAsWebpAsync(ms, new WebpEncoder(), cancellationToken);
            }
            else
            {
                await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 92 }, cancellationToken);
            }

            return ms.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply watermark for event {EventId}. Returning original bytes.", config.EventId);
            return imageBytes;
        }
    }

    // ── Text building ────────────────────────────────────────────────────────

    private static string BuildWatermarkText(WatermarkConfiguration config, WatermarkContext context)
    {
        return config.Mode switch
        {
            WatermarkMode.CustomText =>
                config.CustomText ?? string.Empty,

            WatermarkMode.DynamicTemplate =>
                ResolveTemplate(config.Template ?? string.Empty, context),

            WatermarkMode.StudioBranding =>
                BuildBrandingLines(config, context, includeStudio: true, includeEvent: false),

            WatermarkMode.EventBranding =>
                BuildBrandingLines(config, context, includeStudio: false, includeEvent: true),

            WatermarkMode.StudioAndEvent =>
                BuildBrandingLines(config, context, includeStudio: true, includeEvent: true),

            _ => string.Empty,
        };
    }

    private static string BuildBrandingLines(
        WatermarkConfiguration config,
        WatermarkContext context,
        bool includeStudio,
        bool includeEvent)
    {
        var parts = new List<string>();

        if (includeStudio && config.IncludeStudioName && !string.IsNullOrWhiteSpace(context.StudioName))
        {
            parts.Add(context.StudioName);
        }

        if (includeEvent && config.IncludeEventName)
        {
            parts.Add(context.EventName);
        }

        if (config.IncludeDownloadDate)
        {
            parts.Add(context.DownloadDate.ToString("yyyy-MM-dd"));
        }

        return string.Join("\n", parts);
    }

    private static string ResolveTemplate(string template, WatermarkContext context)
        => template
            .Replace("{StudioName}", context.StudioName ?? string.Empty, StringComparison.Ordinal)
            .Replace("{EventName}", context.EventName, StringComparison.Ordinal)
            .Replace("{EventDate}", context.EventDate.ToString("yyyy-MM-dd"), StringComparison.Ordinal)
            .Replace("{DownloadDate}", context.DownloadDate.ToString("yyyy-MM-dd"), StringComparison.Ordinal)
            .Replace("{DownloadTime}", context.DownloadDate.ToString("HH:mm"), StringComparison.Ordinal)
            .Replace("{PhotoName}", System.IO.Path.GetFileNameWithoutExtension(context.PhotoName), StringComparison.Ordinal)
            .Replace("{SessionId}", context.SessionId ?? string.Empty, StringComparison.Ordinal);

    // ── Font & colour ────────────────────────────────────────────────────────

    private static float CalculateFontSize(int width, int height, WatermarkScale scale)
    {
        var shorter = Math.Min(width, height);
        var percent = scale switch
        {
            WatermarkScale.Small => SmallFontPercent,
            WatermarkScale.Large => LargeFontPercent,
            WatermarkScale.Auto => width > height ? SmallFontPercent : MediumFontPercent,
            _ => MediumFontPercent,
        };
        return Math.Max(12f, shorter * percent);
    }

    private static Font ResolveFont(float fontSize, string? fontName)
    {
        // Build candidate list — user's chosen font is tried first.
        var preferred = string.IsNullOrWhiteSpace(fontName)
            ? new[] { "Arial", "Liberation Sans", "DejaVu Sans", "Helvetica", "Verdana" }
            : new[] { fontName, "Arial", "Liberation Sans", "DejaVu Sans", "Helvetica", "Verdana" };

        FontFamily fontFamily = default;
        foreach (var name in preferred)
        {
            if (SystemFonts.Collection.TryGet(name, out var ff))
            {
                fontFamily = ff;
                break;
            }
        }

        if (fontFamily == default)
        {
            fontFamily = SystemFonts.Families.First();
        }

        return fontFamily.CreateFont(fontSize, FontStyle.Bold);
    }

    private static Color BuildColor(string? textColor, float opacity)
    {
        var alpha = (byte)Math.Clamp(opacity * 255f, 0f, 255f);

        if (!string.IsNullOrWhiteSpace(textColor)
            && Color.TryParse(textColor, out var parsed))
        {
            var rgba = parsed.ToPixel<Rgba32>();
            return Color.FromRgba(rgba.R, rgba.G, rgba.B, alpha);
        }

        // Default: white
        return Color.FromRgba(255, 255, 255, alpha);
    }

    private static Color BuildBgColor(float opacity)
    {
        var alpha = (byte)Math.Clamp(opacity * 255f, 0f, 255f);
        return Color.FromRgba(0, 0, 0, alpha);
    }

    // ── BottomRibbon renderer ─────────────────────────────────────────────────

    /// <summary>
    /// Renders a full-width semi-transparent ribbon at the bottom of the image.
    /// Studio/primary line uses <paramref name="mainFont"/>; event/secondary uses <paramref name="subFont"/>.
    /// Both lines are horizontally centred inside the ribbon.
    /// When <paramref name="logo"/> is supplied the layout is: [Logo] | Studio Name / Event Name.
    /// </summary>
    private static void RenderBottomRibbon(
        IImageProcessingContext ctx,
        string text,
        Font mainFont,
        Font subFont,
        Color textColor,
        Color bgColor,
        int imgWidth,
        int imgHeight,
        int margin,
        Image<Rgba32>? logo)
    {
        // Split \n-joined text into lines (primary = studio name, secondary = event name)
        var lines = text.Split('\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0) return;

        var primary   = lines[0];
        var secondary = lines.Length > 1 ? string.Join("  ·  ", lines.Skip(1)) : null;

        // Measure texts
        var primarySize   = TextMeasurer.MeasureSize(primary, new TextOptions(mainFont));
        var secondarySize = secondary != null
            ? TextMeasurer.MeasureSize(secondary, new TextOptions(subFont))
            : default;

        // Vertical gap between the two lines
        var lineGap    = margin * 0.35f;
        var textBlockH = primarySize.Height
                       + (secondary != null ? lineGap + secondarySize.Height : 0f);

        // Logo: scale height to match full text-block height, width proportionally
        var logoH = 0f;
        var logoW = 0f;
        if (logo != null)
        {
            logoH = textBlockH;
            logoW = logo.Width * (logoH / logo.Height);
        }

        // Ribbon height: text block + vertical padding (1.4× margin on each side)
        var vertPad  = margin * 1.4f;
        var ribbonH  = (int)Math.Ceiling(textBlockH + vertPad * 2f);
        var ribbonTop = imgHeight - ribbonH;

        // ── Draw semi-transparent background ribbon ──────────────────────────
        ctx.Fill(bgColor, new RectangularPolygon(0, ribbonTop, imgWidth, ribbonH));

        // ── Vertical centre of text block within ribbon ──────────────────────
        var contentTopY = ribbonTop + (ribbonH - textBlockH) / 2f;

        if (logo != null)
        {
            // ── [Logo]  Studio Name ──────────────────────────────────────────
            //            Event Name
            var textColW    = Math.Max(primarySize.Width,
                                       secondary != null ? secondarySize.Width : 0f);
            var logoTextGap = margin * 0.75f;
            var unitW       = logoW + logoTextGap + textColW;
            var unitStartX  = (imgWidth - unitW) / 2f;

            // Resize and draw logo centred vertically in ribbon
            logo.Mutate(l => l.Resize((int)Math.Ceiling(logoW), (int)Math.Ceiling(logoH)));
            ctx.DrawImage(logo, new Point((int)unitStartX, (int)contentTopY), 1f);

            var textX = unitStartX + logoW + logoTextGap;

            DrawWithShadow(ctx, primary, mainFont, textColor,
                new PointF(textX, contentTopY), HorizontalAlignment.Left);

            if (secondary != null)
            {
                DrawWithShadow(ctx, secondary, subFont, textColor,
                    new PointF(textX, contentTopY + primarySize.Height + lineGap),
                    HorizontalAlignment.Left);
            }
        }
        else
        {
            // ── Both lines centred in ribbon ─────────────────────────────────
            var cx = imgWidth / 2f;

            DrawWithShadow(ctx, primary, mainFont, textColor,
                new PointF(cx, contentTopY), HorizontalAlignment.Center);

            if (secondary != null)
            {
                DrawWithShadow(ctx, secondary, subFont, textColor,
                    new PointF(cx, contentTopY + primarySize.Height + lineGap),
                    HorizontalAlignment.Center);
            }
        }
    }

    /// <summary>
    /// Draws <paramref name="text"/> with a subtle 1px black shadow for legibility,
    /// followed by the main text in <paramref name="color"/>.
    /// </summary>
    private static void DrawWithShadow(
        IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        PointF origin,
        HorizontalAlignment align)
    {
        var shadowOffset = Math.Max(1f, font.Size * 0.025f);
        var shadow       = Color.FromRgba(0, 0, 0, 130);

        var shadowOpts = new RichTextOptions(font)
        {
            Origin              = new PointF(origin.X + shadowOffset, origin.Y + shadowOffset),
            HorizontalAlignment = align,
            VerticalAlignment   = VerticalAlignment.Top,
        };
        ctx.DrawText(shadowOpts, text, shadow);

        var mainOpts = new RichTextOptions(font)
        {
            Origin              = origin,
            HorizontalAlignment = align,
            VerticalAlignment   = VerticalAlignment.Top,
        };
        ctx.DrawText(mainOpts, text, color);
    }

    // ── Text rendering strategies ────────────────────────────────────────────

    private static void ApplyText(
        IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        WatermarkStyle style,
        int imgWidth,
        int imgHeight,
        int margin)
    {
        switch (style)
        {
            case WatermarkStyle.Corner:
                RenderCorner(ctx, text, font, color, imgWidth, imgHeight, margin);
                break;

            case WatermarkStyle.Center:
                RenderCenter(ctx, text, font, color, imgWidth, imgHeight);
                break;

            case WatermarkStyle.Diagonal:
                RenderDiagonal(ctx, text, font, color, imgWidth, imgHeight);
                break;

            case WatermarkStyle.RepeatedPattern:
                RenderRepeatedPattern(ctx, text, font, color, imgWidth, imgHeight, margin);
                break;

            default:
                RenderCorner(ctx, text, font, color, imgWidth, imgHeight, margin);
                break;
        }
    }

    private static void RenderCorner(
        IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        int imgWidth,
        int imgHeight,
        int margin)
    {
        // RichTextOptions with Bottom/Right alignment correctly positions multi-line text.
        var opts = new RichTextOptions(font)
        {
            Origin = new PointF(imgWidth - margin, imgHeight - margin),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        ctx.DrawText(opts, text, color);
    }

    private static void RenderCenter(
        IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        int imgWidth,
        int imgHeight)
    {
        var opts = new RichTextOptions(font)
        {
            Origin = new PointF(imgWidth / 2f, imgHeight / 2f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ctx.DrawText(opts, text, color);
    }

    private static void RenderDiagonal(
        IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        int imgWidth,
        int imgHeight)
    {
        var cx = imgWidth / 2f;
        var cy = imgHeight / 2f;

        var measured = TextMeasurer.MeasureSize(text, new TextOptions(font));
        var x = cx - measured.Width / 2f;
        var y = cy - measured.Height / 2f;

        var drawingOptions = new DrawingOptions
        {
            Transform = Matrix3x2.CreateRotation(-MathF.PI / 4f, new Vector2(cx, cy)),
        };

        ctx.DrawText(drawingOptions, text, font, color, new PointF(x, y));
    }

    private static void RenderRepeatedPattern(
        IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        int imgWidth,
        int imgHeight,
        int margin)
    {
        var measured = TextMeasurer.MeasureSize(text, new TextOptions(font));
        var stepX = (int)(measured.Width + margin * 4);
        var stepY = (int)(measured.Height + margin * 4);

        for (var row = 0; row * stepY < imgHeight + stepY; row++)
        {
            for (var col = 0; col * stepX < imgWidth + stepX; col++)
            {
                var x = col * stepX + margin;
                var y = row * stepY + margin;
                var pivot = new Vector2(x + measured.Width / 2f, y + measured.Height / 2f);

                var drawingOptions = new DrawingOptions
                {
                    Transform = Matrix3x2.CreateRotation(-MathF.PI / 6f, pivot),
                };

                ctx.DrawText(drawingOptions, text, font, color, new PointF(x, y));
            }
        }
    }

    // ── Logo overlay ─────────────────────────────────────────────────────────

    private static async Task OverlayLogoAsync(
        Image<Rgba32> image,
        WatermarkConfiguration config,
        CancellationToken cancellationToken)
    {
        using var logo = await Image.LoadAsync<Rgba32>(config.LogoPath!, cancellationToken);

        var targetWidth = (int)(image.Width * LogoWidthPercent);
        var targetHeight = (int)(logo.Height * ((float)targetWidth / logo.Width));

        logo.Mutate(ctx => ctx.Resize(targetWidth, targetHeight));
        logo.Mutate(ctx => ctx.Opacity(config.Opacity));

        var margin = (int)(Math.Min(image.Width, image.Height) * MarginPercent);
        var x = image.Width - targetWidth - margin;
        var y = image.Height - targetHeight - margin;

        image.Mutate(ctx => ctx.DrawImage(logo, new Point(x, y), 1f));
    }
}

