namespace EventPhoto.Contracts.Responses.FaceSearch;

/// <summary>
/// Current state of a guest face-search session — returned by the status endpoint
/// and also sent as a SignalR payload for <c>face-search-progress</c> / <c>face-search-completed</c>.
/// </summary>
public sealed record FaceSearchStatusResponse(
    string SessionToken,
    string Status,
    int MatchCount,
    DateTimeOffset ExpiresAt,
    string? ErrorMessage = null);
