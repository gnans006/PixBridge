namespace EventPhoto.Contracts.Responses.FaceSearch;

/// <summary>
/// A single matched photo returned by a "Find My Photos™" face-search session.
/// Raw similarity scores are NEVER exposed. Only <see cref="ConfidenceLabel"/> is shown to guests.
/// </summary>
public sealed record FaceSearchMatchResponse(
    Guid PhotoId,
    string ThumbnailUrl,
    string DownloadUrl,
    float SimilarityScore,
    DateTimeOffset CapturedAt,
    string FileName,
    /// <summary>Guest-facing confidence label: "Excellent Match", "Strong Match", or "Possible Match".</summary>
    string ConfidenceLabel = "Possible Match",
    /// <summary>Internal category for UI styling (Excellent/Strong/Possible).</summary>
    string ConfidenceCategory = "Possible");

/// <summary>Paged results for a completed "Find My Photos™" session.</summary>
public sealed record FaceSearchResultResponse(
    string SessionToken,
    int TotalMatches,
    IReadOnlyList<FaceSearchMatchResponse> Matches,
    int Page,
    int PageSize,
    bool HasNextPage,
    /// <summary>Search duration in milliseconds for performance monitoring.</summary>
    int SearchDurationMs = 0);
