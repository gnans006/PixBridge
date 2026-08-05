namespace EventPhoto.Contracts.Responses.Statistics;

/// <summary>Spotlight data for the most active event on the dashboard.</summary>
public sealed record EventSpotlightResponse(
    Guid EventId,
    string Name,
    string EventType,
    DateOnly EventDate,
    string? ClientName,
    string? VenueName,
    int PhotoCount,
    int TotalDownloads,
    long StorageBytes,
    string StorageHuman,
    bool FaceRecognitionEnabled,
    bool WatermarkEnabled,
    bool IsActive,
    string? FirstThumbnailUrl);
