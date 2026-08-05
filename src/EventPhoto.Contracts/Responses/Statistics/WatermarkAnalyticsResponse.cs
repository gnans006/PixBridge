namespace EventPhoto.Contracts.Responses.Statistics;

/// <summary>Watermark analytics across all events.</summary>
public sealed record WatermarkAnalyticsResponse(
    int EventsWithWatermark,
    int TotalEvents,
    int TotalDownloads,
    int ProtectedDownloads,
    double CoveragePercentage,
    int ActiveWatermarkEvents);
