namespace EventPhoto.Contracts.Requests.FaceSearch;

/// <summary>
/// Payload sent when a guest uploads their selfie to start a face-search session.
/// The selfie image is sent as a multipart/form-data file field named <c>selfie</c>.
/// </summary>
public sealed record StartFaceSearchRequest
{
    /// <summary>The event to search against.</summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Optional override for the cosine-similarity threshold.
    /// When omitted, the event's configured <c>FaceMatchThreshold</c> is used.
    /// </summary>
    public float? ThresholdOverride { get; init; }
}
