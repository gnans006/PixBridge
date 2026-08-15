namespace EventPhoto.Contracts.Responses.Settings;

/// <summary>API response for the application settings.</summary>
public sealed record ApplicationSettingsResponse(
    Guid Id,
    string StudioName,
    string ServerName,
    string PublicBaseUrl,
    int ServerPort,
    string DefaultEventGalleryMode,
    bool EnableWatermarkByDefault,
    bool EnableFaceRecognitionByDefault,
    // Feature flags
    bool IsWatermarkEnabled,
    bool IsFaceSearchEnabled,
    // Phase 6 — Studio Profile
    string? Phone,
    string? Email,
    string? Website,
    string? Address,
    string? Instagram,
    string? Facebook,
    string? WhatsApp,
    string? LogoPath,
    string? GstNumber,
    // Phase 7 — Branding
    string PrimaryColor,
    string SecondaryColor,
    string BrandTheme,
    string GalleryTheme,
    string QrTheme,
    Guid? DefaultWatermarkProfileId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
