namespace EventPhoto.Contracts.Responses.Statistics;

/// <summary>Per-event face recognition breakdown item.</summary>
public sealed record FaceAnalyticsEventItem(
    Guid EventId,
    string EventName,
    int PhotoCount,
    int FaceEmbeddings);

/// <summary>Face recognition analytics across all events.</summary>
public sealed record FaceAnalyticsResponse(
    int TotalIndexedFaces,
    int TotalPendingPhotos,
    int EventsWithFaceSearch,
    List<FaceAnalyticsEventItem> EventBreakdown);
