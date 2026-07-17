using EventPhoto.Contracts.Responses.FaceSearch;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.FaceSearch.Queries;

/// <summary>
/// Returns the paged list of matched photos for a completed guest face-search session.
/// </summary>
public sealed record GetMatchedPhotosQuery(
    string SessionToken,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<FaceSearchResultResponse>>;

/// <summary>Handles <see cref="GetMatchedPhotosQuery"/>.</summary>
public sealed class GetMatchedPhotosQueryHandler(
    IGuestFaceSessionRepository sessionRepository,
    IPhotoMatchRepository matchRepository,
    IPhotoRepository photoRepository,
    ISystemSettingRepository settingRepository)
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

        var serverUrl = await settingRepository.GetValueAsync("app.serverUrl", cancellationToken) ?? "http://localhost:5000";
        var baseUrl = serverUrl.TrimEnd('/');

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
                return new FaceSearchMatchResponse(
                    photo.Id,
                    $"{baseUrl}/api/photos/{photo.Id}/thumbnail",
                    $"{baseUrl}/api/photos/{photo.Id}/download?sessionToken={request.SessionToken}",
                    m.SimilarityScore,
                    photo.CapturedAt,
                    photo.FileName);
            })
            .ToList();

        return Result.Success(new FaceSearchResultResponse(
            session.SessionToken,
            total,
            matchResponses,
            request.Page,
            request.PageSize,
            request.Page * request.PageSize < total));
    }
}
