using EventPhoto.Contracts.Responses.FaceSearch;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.FaceSearch.Queries;

/// <summary>
/// Returns the paged list of matched photos for a completed "Find My Photos™" session.
/// Raw similarity scores are converted to human-friendly confidence labels.
/// </summary>
public sealed record GetMatchedPhotosQuery(
    string SessionToken,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<FaceSearchResultResponse>>;

/// <summary>Handles <see cref="GetMatchedPhotosQuery"/>.</summary>
public sealed class GetMatchedPhotosQueryHandler(
    IGuestFaceSessionRepository sessionRepository,
    IPhotoMatchRepository matchRepository,
    IPhotoRepository photoRepository)
    : IRequestHandler<GetMatchedPhotosQuery, Result<FaceSearchResultResponse>>
{
    public async Task<Result<FaceSearchResultResponse>> Handle(
        GetMatchedPhotosQuery request,
        CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByTokenAsync(request.SessionToken, cancellationToken);
        if (session is null)
            return Result.Failure<FaceSearchResultResponse>("Session not found.");

        if (session.IsExpired)
            return Result.Failure<FaceSearchResultResponse>("Session has expired.");

        var matches = await matchRepository.GetPagedBySessionIdAsync(
            session.Id, request.Page, request.PageSize, cancellationToken);

        var total = await matchRepository.CountBySessionIdAsync(session.Id, cancellationToken);

        var photoIds = matches.Select(m => m.PhotoId).ToList();
        var photos = await photoRepository.GetByIdsAsync(photoIds, cancellationToken);
        var photoLookup = photos.ToDictionary(p => p.Id);

        var matchResponses = matches
            .Where(m => photoLookup.ContainsKey(m.PhotoId))
            .Select(m =>
            {
                var photo = photoLookup[m.PhotoId];
                var (label, category) = GetConfidenceDisplay(m.SimilarityScore);
                return new FaceSearchMatchResponse(
                    photo.Id,
                    $"/api/photos/{photo.Id}/thumbnail",
                    $"/api/photos/{photo.Id}/download?sessionToken={request.SessionToken}",
                    m.SimilarityScore,
                    photo.CapturedAt,
                    photo.FileName,
                    label,
                    category);
            })
            .ToList();

        return Result.Success(new FaceSearchResultResponse(
            session.SessionToken,
            total,
            matchResponses,
            request.Page,
            request.PageSize,
            request.Page * request.PageSize < total,
            session.SearchDurationMs));
    }

    /// <summary>
    /// Maps a cosine similarity score to a guest-facing label and CSS category.
    /// Raw numeric scores are NEVER exposed to guests.
    /// </summary>
    private static (string Label, string Category) GetConfidenceDisplay(float similarity)
        => similarity switch
        {
            >= 0.90f => ("Excellent Match", "Excellent"),
            >= 0.80f => ("Strong Match", "Strong"),
            _ => ("Possible Match", "Possible")
        };
}

