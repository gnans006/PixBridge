using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Stores the per-event watermark settings applied at download time.
/// A single row exists per event (1:1 with <see cref="Event"/>).
/// </summary>
public sealed class WatermarkConfiguration : AggregateRoot
{
    private WatermarkConfiguration()
    {
    }

    /// <summary>Gets the identifier of the parent event.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Gets a value indicating whether watermarking is active for this event.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Gets the content source used to generate the watermark text.</summary>
    public WatermarkMode Mode { get; private set; } = WatermarkMode.StudioBranding;

    /// <summary>Gets the visual placement style for the watermark.</summary>
    public WatermarkStyle Style { get; private set; } = WatermarkStyle.Corner;

    /// <summary>
    /// Gets the opacity of the watermark layer (0.0 = fully transparent, 1.0 = fully opaque).
    /// </summary>
    public float Opacity { get; private set; } = 0.6f;

    /// <summary>Gets the relative size of the watermark relative to the image dimensions.</summary>
    public WatermarkScale Scale { get; private set; } = WatermarkScale.Medium;

    /// <summary>
    /// Gets the static text rendered when <see cref="Mode"/> is <see cref="WatermarkMode.CustomText"/>.
    /// </summary>
    public string? CustomText { get; private set; }

    /// <summary>
    /// Gets the tokenised template string rendered when <see cref="Mode"/> is
    /// <see cref="WatermarkMode.DynamicTemplate"/>.
    /// Supports: {StudioName} {EventName} {EventDate} {DownloadDate} {DownloadTime}
    ///           {PhotoName} {SessionId}
    /// </summary>
    public string? Template { get; private set; }

    /// <summary>
    /// Gets the absolute path to a logo image overlaid on the watermark when a branding
    /// mode is selected. <see langword="null"/> means no logo.
    /// </summary>
    public string? LogoPath { get; private set; }

    /// <summary>
    /// When <see langword="true"/> and a branding mode is active, the studio name from
    /// system settings is included in the watermark text.
    /// </summary>
    public bool IncludeStudioName { get; private set; } = true;

    /// <summary>
    /// When <see langword="true"/> and a branding mode is active, the event name is
    /// appended to the watermark text.
    /// </summary>
    public bool IncludeEventName { get; private set; }

    /// <summary>
    /// When <see langword="true"/>, the UTC download date is appended to the watermark text.
    /// </summary>
    public bool IncludeDownloadDate { get; private set; }

    /// <summary>
    /// When <see langword="true"/> the watermark is applied during the download pipeline.
    /// Set to <see langword="false"/> to temporarily bypass watermarking without deleting the config.
    /// </summary>
    public bool ApplyOnDownload { get; private set; } = true;

    /// <summary>
    /// Gets the hex colour of the watermark text (e.g. <c>#FFFFFF</c>).
    /// The <see cref="Opacity"/> value is applied as the alpha channel at render time.
    /// </summary>
    public string TextColor { get; private set; } = "#FFFFFF";

    /// <summary>
    /// Gets the font-family name to use for the watermark text (e.g. <c>Arial</c>).
    /// When <see langword="null"/> or empty the service selects the best available system font.
    /// </summary>
    public string? FontName { get; private set; }

    /// <summary>
    /// Gets the opacity of the semi-transparent ribbon background used by
    /// <see cref="WatermarkStyle.BottomRibbon"/> (0.0 = transparent, 1.0 = opaque).
    /// Ignored for other styles.
    /// </summary>
    public float BackgroundOpacity { get; private set; } = 0.20f;

    /// <summary>
    /// When <see langword="true"/>, the watermark is also applied when the image is loaded
    /// for inline preview (lightbox) in addition to explicit downloads.
    /// </summary>
    public bool ApplyOnPreview { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a default (disabled) watermark configuration for the specified event.
    /// </summary>
    /// <param name="eventId">The owning event identifier.</param>
    /// <returns>A new <see cref="WatermarkConfiguration"/> with sensible defaults.</returns>
    public static WatermarkConfiguration CreateForEvent(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException("EventId is required when creating a watermark configuration.");
        }

        return new WatermarkConfiguration
        {
            EventId = eventId,
            Enabled = false,
            Mode = WatermarkMode.StudioAndEvent,
            Style = WatermarkStyle.BottomRibbon,
            Opacity = 0.85f,
            Scale = WatermarkScale.Auto,
            IncludeStudioName = true,
            IncludeEventName = true,
            IncludeDownloadDate = false,
            ApplyOnDownload = true,
            ApplyOnPreview = false,
            TextColor = "#FFFFFF",
            FontName = "Montserrat",
            BackgroundOpacity = 0.20f,
        };
    }

    // ── Behaviour ────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates all watermark settings.
    /// </summary>
    public void Update(
        bool enabled,
        WatermarkMode mode,
        WatermarkStyle style,
        float opacity,
        WatermarkScale scale,
        string? customText,
        string? template,
        string? logoPath,
        bool includeStudioName,
        bool includeEventName,
        bool includeDownloadDate,
        bool applyOnDownload,
        string textColor,
        string? fontName,
        float backgroundOpacity,
        bool applyOnPreview)
    {
        if (opacity is < 0f or > 1f)
        {
            throw new DomainException("Opacity must be between 0.0 and 1.0.");
        }

        if (backgroundOpacity is < 0f or > 1f)
        {
            throw new DomainException("BackgroundOpacity must be between 0.0 and 1.0.");
        }

        if (mode == WatermarkMode.CustomText && string.IsNullOrWhiteSpace(customText))
        {
            throw new DomainException("CustomText is required when Mode is CustomText.");
        }

        if (mode == WatermarkMode.DynamicTemplate && string.IsNullOrWhiteSpace(template))
        {
            throw new DomainException("Template is required when Mode is DynamicTemplate.");
        }

        Enabled = enabled;
        Mode = mode;
        Style = style;
        Opacity = opacity;
        Scale = scale;
        CustomText = customText?.Trim();
        Template = template?.Trim();
        LogoPath = logoPath?.Trim();
        IncludeStudioName = includeStudioName;
        IncludeEventName = includeEventName;
        IncludeDownloadDate = includeDownloadDate;
        ApplyOnDownload = applyOnDownload;
        TextColor = string.IsNullOrWhiteSpace(textColor) ? "#FFFFFF" : textColor.Trim();
        FontName = string.IsNullOrWhiteSpace(fontName) ? null : fontName.Trim();
        BackgroundOpacity = backgroundOpacity;
        ApplyOnPreview = applyOnPreview;
        Touch();
    }
}
