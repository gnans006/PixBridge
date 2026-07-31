namespace EventPhoto.Contracts.Requests.Events;

/// <summary>
/// Request body for creating or updating an event's watermark configuration.
/// </summary>
public sealed record UpsertWatermarkConfigRequest(
    bool Enabled,
    string Mode,
    string Style,
    float Opacity,
    string Scale,
    string? CustomText,
    string? Template,
    string? LogoPath,
    bool IncludeStudioName,
    bool IncludeEventName,
    bool IncludeDownloadDate,
    bool ApplyOnDownload,
    string TextColor,
    string? FontName,
    float BackgroundOpacity,
    bool ApplyOnPreview);
