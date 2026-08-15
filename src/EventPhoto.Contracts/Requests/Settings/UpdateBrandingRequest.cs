namespace EventPhoto.Contracts.Requests.Settings;

/// <summary>Request body for updating branding settings.</summary>
public sealed record UpdateBrandingRequest(
    string PrimaryColor,
    string SecondaryColor,
    string BrandTheme,
    string GalleryTheme,
    string QrTheme,
    Guid? DefaultWatermarkProfileId);
