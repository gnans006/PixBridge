namespace EventPhoto.Contracts.Responses.FaceSearch;

/// <summary>A single matched photo returned by a face-search session.</summary>
public sealed record FaceSearchMatchResponse(
    Guid PhotoId,
    string ThumbnailUrl,
    string DownloadUrl,
    float SimilarityScore,
    DateTimeOffset CapturedAt,
    string FileName);

/// <summary>Paged results for a completed face-search session.</summary>
public sealed record FaceSearchResultResponse(
    string SessionToken,
    int TotalMatches,
    IReadOnlyList<FaceSearchMatchResponse> Matches,
    int Page,
    int PageSize,
    bool HasNextPage);
