namespace EventPhoto.Contracts.Requests.Settings;

/// <summary>Request body for updating studio profile fields.</summary>
public sealed record UpdateStudioProfileRequest(
    string? Phone,
    string? Email,
    string? Website,
    string? Address,
    string? Instagram,
    string? Facebook,
    string? WhatsApp,
    string? LogoPath,
    string? GstNumber);
