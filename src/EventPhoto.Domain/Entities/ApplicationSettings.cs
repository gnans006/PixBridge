using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Central application configuration aggregate (single-row pattern).
/// Only one active record ever exists. <see cref="SingletonId"/> is always used as the primary key.
/// </summary>
public sealed class ApplicationSettings : AggregateRoot
{
    /// <summary>Well-known fixed ID for the singleton application-settings record.</summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    private ApplicationSettings()
    {
    }

    /// <summary>Gets the display name of the photography studio.</summary>
    public string StudioName { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable server/machine name shown in the UI.</summary>
    public string ServerName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the canonical public base URL used for all generated links and QR codes.
    /// Must start with http:// or https://. Never ends with a trailing slash.
    /// </summary>
    public string PublicBaseUrl { get; private set; } = string.Empty;

    /// <summary>Gets the TCP port on which the API server listens.</summary>
    public int ServerPort { get; private set; }

    /// <summary>Gets the default gallery mode applied to newly created events.</summary>
    public GalleryMode DefaultEventGalleryMode { get; private set; }

    /// <summary>Gets whether watermarking should be enabled by default on new events.</summary>
    public bool EnableWatermarkByDefault { get; private set; }

    /// <summary>Gets whether face recognition should be enabled by default on new events.</summary>
    public bool EnableFaceRecognitionByDefault { get; private set; }

    // ── Feature Flags ─────────────────────────────────────────────────────────

    /// <summary>Gets whether the Watermark module is globally enabled for this studio.</summary>
    public bool IsWatermarkEnabled { get; private set; } = true;

    /// <summary>Gets whether the Face Search / AI module is globally enabled for this studio.</summary>
    public bool IsFaceSearchEnabled { get; private set; } = true;

    // ── Phase 6 — Studio Profile ──────────────────────────────────────────────

    /// <summary>Gets the studio's contact phone number.</summary>
    public string? Phone { get; private set; }

    /// <summary>Gets the studio's contact email address.</summary>
    public string? Email { get; private set; }

    /// <summary>Gets the studio's website URL.</summary>
    public string? Website { get; private set; }

    /// <summary>Gets the studio's physical address.</summary>
    public string? Address { get; private set; }

    /// <summary>Gets the studio's Instagram handle or URL.</summary>
    public string? Instagram { get; private set; }

    /// <summary>Gets the studio's Facebook page URL.</summary>
    public string? Facebook { get; private set; }

    /// <summary>Gets the studio's WhatsApp number or link.</summary>
    public string? WhatsApp { get; private set; }

    /// <summary>Gets the file-system path of the studio logo.</summary>
    public string? LogoPath { get; private set; }

    /// <summary>Gets the studio's GST / tax registration number.</summary>
    public string? GstNumber { get; private set; }

    // ── Phase 7 — Branding ────────────────────────────────────────────────────

    /// <summary>Gets the primary brand colour (hex, e.g. "#6366f1").</summary>
    public string PrimaryColor { get; private set; } = "#6366f1";

    /// <summary>Gets the secondary brand colour (hex).</summary>
    public string SecondaryColor { get; private set; } = "#8b5cf6";

    /// <summary>Gets the preferred UI theme: "dark", "light", "midnight", or "system".</summary>
    public string BrandTheme { get; private set; } = "dark";

    /// <summary>Gets the gallery page theme variant (e.g. "minimal", "masonry", "cinematic").</summary>
    public string GalleryTheme { get; private set; } = "minimal";

    /// <summary>Gets the QR code visual theme (e.g. "standard", "branded", "elegant").</summary>
    public string QrTheme { get; private set; } = "standard";

    /// <summary>Gets the default watermark profile applied to new events.</summary>
    public Guid? DefaultWatermarkProfileId { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the default settings record. Uses <see cref="SingletonId"/> as the primary key
    /// and seeds sensible defaults so the application starts without manual configuration.
    /// </summary>
    public static ApplicationSettings CreateDefault()
    {
        return new ApplicationSettings
        {
            Id = SingletonId,
            StudioName = "My Photography Studio",
            ServerName = Environment.MachineName,
            PublicBaseUrl = "http://localhost:5000",
            ServerPort = 5000,
            DefaultEventGalleryMode = GalleryMode.GalleryOnly,
            EnableWatermarkByDefault = false,
            EnableFaceRecognitionByDefault = false,
            IsWatermarkEnabled = true,
            IsFaceSearchEnabled = true,
            PrimaryColor = "#6366f1",
            SecondaryColor = "#8b5cf6",
            BrandTheme = "dark",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Updates all configurable settings.
    /// Domain invariants are validated in the entity to guarantee consistency regardless of caller.
    /// </summary>
    public void Update(
        string studioName,
        string serverName,
        string publicBaseUrl,
        int serverPort,
        GalleryMode defaultEventGalleryMode,
        bool enableWatermarkByDefault,
        bool enableFaceRecognitionByDefault,
        bool isWatermarkEnabled,
        bool isFaceSearchEnabled)
    {
        if (string.IsNullOrWhiteSpace(studioName))
            throw new DomainException("Studio name is required.");
        if (studioName.Length > 200)
            throw new DomainException("Studio name must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(serverName))
            throw new DomainException("Server name is required.");
        if (serverName.Length > 100)
            throw new DomainException("Server name must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            throw new DomainException("Public base URL is required.");
        if (!Uri.TryCreate(publicBaseUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("Public base URL must be a valid http:// or https:// URL.");

        if (serverPort is < 1 or > 65535)
            throw new DomainException("Server port must be between 1 and 65535.");

        StudioName = studioName.Trim();
        ServerName = serverName.Trim();
        PublicBaseUrl = publicBaseUrl.Trim().TrimEnd('/');
        ServerPort = serverPort;
        DefaultEventGalleryMode = defaultEventGalleryMode;
        EnableWatermarkByDefault = enableWatermarkByDefault;
        EnableFaceRecognitionByDefault = enableFaceRecognitionByDefault;
        IsWatermarkEnabled = isWatermarkEnabled;
        IsFaceSearchEnabled = isFaceSearchEnabled;
        Touch();
    }

    /// <summary>
    /// Updates studio profile contact and social media fields.
    /// </summary>
    public void UpdateStudioProfile(
        string? phone,
        string? email,
        string? website,
        string? address,
        string? instagram,
        string? facebook,
        string? whatsApp,
        string? logoPath,
        string? gstNumber)
    {
        if (email is not null && email.Length > 200)
            throw new DomainException("Email must not exceed 200 characters.");
        if (website is not null && website.Length > 2048)
            throw new DomainException("Website URL must not exceed 2048 characters.");
        if (gstNumber is not null && gstNumber.Length > 50)
            throw new DomainException("GST number must not exceed 50 characters.");

        Phone = phone?.Trim();
        Email = email?.Trim();
        Website = website?.Trim();
        Address = address?.Trim();
        Instagram = instagram?.Trim();
        Facebook = facebook?.Trim();
        WhatsApp = whatsApp?.Trim();
        LogoPath = logoPath;
        GstNumber = gstNumber?.Trim();
        Touch();
    }

    /// <summary>
    /// Updates branding colors and theme.
    /// </summary>
    public void UpdateBranding(
        string primaryColor,
        string secondaryColor,
        string brandTheme,
        string galleryTheme,
        string qrTheme,
        Guid? defaultWatermarkProfileId)
    {
        static bool IsHex(string? s) =>
            s is { Length: >= 4 and <= 7 } && s.StartsWith('#');

        if (!IsHex(primaryColor))
            throw new DomainException("Primary color must be a valid hex color (e.g. #6366f1).");
        if (!IsHex(secondaryColor))
            throw new DomainException("Secondary color must be a valid hex color.");
        if (brandTheme is not ("dark" or "light" or "midnight" or "system"))
            throw new DomainException("Brand theme must be 'dark', 'light', 'midnight', or 'system'.");

        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        BrandTheme = brandTheme;
        GalleryTheme = galleryTheme?.Trim() ?? "minimal";
        QrTheme = qrTheme?.Trim() ?? "standard";
        DefaultWatermarkProfileId = defaultWatermarkProfileId;
        Touch();
    }

    /// <summary>
    /// Updates only the PublicBaseUrl. Used by startup IP auto-detection without changing other fields.
    /// </summary>
    public void UpdatePublicBaseUrl(string publicBaseUrl)
    {
        if (!Uri.TryCreate(publicBaseUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("Public base URL must be a valid http:// or https:// URL.");

        PublicBaseUrl = publicBaseUrl.Trim().TrimEnd('/');
        Touch();
    }
}
