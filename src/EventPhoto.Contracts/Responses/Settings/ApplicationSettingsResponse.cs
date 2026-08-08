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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
