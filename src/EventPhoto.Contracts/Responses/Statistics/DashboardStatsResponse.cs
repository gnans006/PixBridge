namespace EventPhoto.Contracts.Responses.Statistics;

/// <summary>Aggregate statistics for the admin dashboard.</summary>
public sealed record DashboardStatsResponse(
    int TotalEvents,
    int ActiveEvents,
    int TotalPhotos,
    int TotalDownloads,
    long TotalStorageBytes,
    string TotalStorageHuman);
