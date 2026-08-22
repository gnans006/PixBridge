using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.FaceSearch.Commands;

/// <summary>
/// Deletes <see cref="Domain.Entities.PhotoMatch"/> rows that belong to sessions
/// which expired more than <see cref="RetentionDays"/> ago.
///
/// <para>PhotoMatch rows are safe to delete once the corresponding
/// <see cref="Domain.Entities.GuestFaceSession"/> is expired — guests can no longer
/// retrieve results for expired sessions, so the match data has no further use.</para>
///
/// <para>Run periodically by <c>SelfieRetentionService</c> in the Worker.</para>
/// </summary>
/// <param name="RetentionDays">
/// Match rows whose session expired more than this many days ago will be deleted.
/// Defaults to 1 day, giving guests a short window after expiry before rows are cleaned up.
/// </param>
public sealed record CleanupExpiredPhotoMatchesCommand(int RetentionDays = 1)
    : IRequest<Result<int>>;

/// <summary>Handles <see cref="CleanupExpiredPhotoMatchesCommand"/>.</summary>
public sealed class CleanupExpiredPhotoMatchesCommandHandler(
    IGuestFaceSessionRepository sessionRepository,
    IPhotoMatchRepository matchRepository,
    IUnitOfWork unitOfWork,
    ILogger<CleanupExpiredPhotoMatchesCommandHandler> logger)
    : IRequestHandler<CleanupExpiredPhotoMatchesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        CleanupExpiredPhotoMatchesCommand request,
        CancellationToken cancellationToken)
    {
        // Find IDs of sessions that were expired more than RetentionDays ago.
        // Sessions expired within the retention window are kept so guests can
        // still retrieve their results during a brief post-expiry grace window.
        var expiredBefore = DateTimeOffset.UtcNow.AddDays(-request.RetentionDays);

        var sessionIds = await sessionRepository.GetExpiredSessionIdsByExpiryAsync(
            expiredBefore, cancellationToken);

        if (sessionIds.Count == 0)
            return Result.Success(0);

        var deleted = await matchRepository.DeleteBySessionIdsAsync(sessionIds, cancellationToken);

        if (deleted > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "PhotoMatch cleanup: deleted {Rows} match row(s) from {Sessions} expired session(s).",
                deleted, sessionIds.Count);
        }

        return Result.Success(deleted);
    }
}
