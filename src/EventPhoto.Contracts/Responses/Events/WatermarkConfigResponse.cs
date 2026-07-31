namespace EventPhoto.Contracts.Responses.Events;

/// <summary>
/// API response DTO for a watermark configuration.
/// </summary>
public sealed record WatermarkConfigResponse(
    Guid Id,
    Guid EventId,
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
    bool ApplyOnPreview,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
