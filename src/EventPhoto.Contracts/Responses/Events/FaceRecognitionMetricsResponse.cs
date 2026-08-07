namespace EventPhoto.Contracts.Responses.Events;

/// <summary>Face recognition indexing and search metrics for a single event.</summary>
public sealed record FaceRecognitionMetricsResponse(
    Guid EventId,
    bool Enabled,
    float MatchThreshold,
    int TotalPhotos,
    int IndexedFaces,
    int IndexedPhotos,
    int PendingPhotos,
    int FailedPhotos);
