using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.AiDiscovery.Commands;

/// <summary>
/// Purges selfie embeddings from expired <see cref="Domain.Entities.GuestFaceSession"/> records
/// that have not yet been zeroed out.
///
/// <para>Called periodically by <c>SelfieRetentionService</c> to enforce the configured
/// privacy retention policy (default: 24 hours after session expiry).</para>
/// </summary>
public sealed record PurgeExpiredSelfiesCommand(
    /// <summary>Purge selfies from sessions that expired at least this many hours ago.</summary>
    int RetentionHours = 24) : IRequest<Result<int>>;

/// <summary>Handles <see cref="PurgeExpiredSelfiesCommand"/>.</summary>
public sealed class PurgeExpiredSelfiesCommandHandler(
    IGuestFaceSessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<PurgeExpiredSelfiesCommandHandler> logger)
    : IRequestHandler<PurgeExpiredSelfiesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        PurgeExpiredSelfiesCommand request,
        CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-request.RetentionHours);
        var sessions = await sessionRepository.GetExpiredWithEmbeddingAsync(cutoff, cancellationToken);

        if (sessions.Count == 0)
            return Result.Success(0);

        foreach (var session in sessions)
        {
            session.PurgeSelfieEmbedding();
            await sessionRepository.UpdateAsync(session, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Purged selfie embeddings from {Count} expired session(s) (retention={Hours}h).",
            sessions.Count, request.RetentionHours);

        return Result.Success(sessions.Count);
    }
}
