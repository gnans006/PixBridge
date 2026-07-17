using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.FaceSearch.Commands;

/// <summary>
/// Marks all expired <see cref="Domain.Entities.GuestFaceSession"/> records as
/// <see cref="Domain.Enums.FaceSessionStatus.Expired"/>.
/// Run periodically by the Worker's session-expiry cleanup job.
/// </summary>
public sealed record ExpireFaceSessionsCommand : IRequest<Result<int>>;

/// <summary>Handles <see cref="ExpireFaceSessionsCommand"/>.</summary>
public sealed class ExpireFaceSessionsCommandHandler(
    IGuestFaceSessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<ExpireFaceSessionsCommandHandler> logger)
    : IRequestHandler<ExpireFaceSessionsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(ExpireFaceSessionsCommand request, CancellationToken cancellationToken)
    {
        var expired = await sessionRepository.GetExpiredSessionsAsync(cancellationToken);
        foreach (var session in expired)
        {
            session.MarkExpired();
            await sessionRepository.UpdateAsync(session, cancellationToken);
        }

        if (expired.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Expired {Count} guest face session(s).", expired.Count);
        }

        return Result.Success(expired.Count);
    }
}
