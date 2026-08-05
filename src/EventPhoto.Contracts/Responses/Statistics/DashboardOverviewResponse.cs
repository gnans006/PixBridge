namespace EventPhoto.Contracts.Responses.Statistics;

/// <summary>Consolidated KPI overview for the admin dashboard command centre.</summary>
public sealed record DashboardOverviewResponse(
    int ActiveEvents,
    int TotalEvents,
    int TotalPhotos,
    int DownloadsToday,
    int TotalDownloads,
    long TotalSizeBytes,
    string TotalSizeHuman,
    int PendingThumbnails,
    int PendingFaceIndexes,
    int TotalFaceEmbeddings,
    int EventsWithFaceSearch,
    int EventsWithWatermark);
