using EventPhoto.Contracts.Responses.FaceSearch;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.FaceSearch.Queries;

/// <summary>
/// Returns the current status of a guest face-search session by its token.
/// </summary>
public sealed record GetFaceSearchStatusQuery(string SessionToken) : IRequest<Result<FaceSearchStatusResponse>>;

/// <summary>Handles <see cref="GetFaceSearchStatusQuery"/>.</summary>
public sealed class GetFaceSearchStatusQueryHandler(IGuestFaceSessionRepository sessionRepository)
    : IRequestHandler<GetFaceSearchStatusQuery, Result<FaceSearchStatusResponse>>
{
    public async Task<Result<FaceSearchStatusResponse>> Handle(
        GetFaceSearchStatusQuery request,
        CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByTokenAsync(request.SessionToken, cancellationToken);
        if (session is null)
            return Result.Failure<FaceSearchStatusResponse>("Session not found or has expired.");

        return Result.Success(new FaceSearchStatusResponse(
            session.SessionToken,
            session.Status.ToString(),
            session.MatchCount,
            session.ExpiresAt,
            session.Status == FaceSessionStatus.Expired ? "Session has expired." : null));
    }
}
