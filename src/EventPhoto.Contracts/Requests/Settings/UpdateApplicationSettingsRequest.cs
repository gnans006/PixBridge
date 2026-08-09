namespace EventPhoto.Contracts.Requests.Settings;

/// <summary>Request body for updating application settings.</summary>
public sealed record UpdateApplicationSettingsRequest(
    string StudioName,
    string ServerName,
    string PublicBaseUrl,
    int ServerPort,
    string DefaultEventGalleryMode,
    bool EnableWatermarkByDefault,
    bool EnableFaceRecognitionByDefault,
    bool IsWatermarkEnabled,
    bool IsFaceSearchEnabled);
