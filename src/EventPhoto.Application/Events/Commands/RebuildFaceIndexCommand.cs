using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Events.Commands;

/// <summary>
/// Re-queues photos of a face-recognition-enabled event for face indexing.
/// The background worker will pick them up and process them asynchronously.
/// </summary>
/// <param name="EventId">The event to rebuild face index for.</param>
/// <param name="Force">
/// When <c>false</c> (default), only photos with no existing embeddings are queued —
/// already-indexed photos are skipped to avoid expensive re-inference on every photo.
/// When <c>true</c>, every photo is re-queued regardless (use for model upgrades).
/// </param>
public sealed record RebuildFaceIndexCommand(Guid EventId, bool Force = false) : IRequest<Result<int>>;

/// <summary>Handles <see cref="RebuildFaceIndexCommand"/>.</summary>
public sealed class RebuildFaceIndexCommandHandler(
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
    IFaceEmbeddingRepository embeddingRepository,
    IUnitOfWork unitOfWork,
    ILogger<RebuildFaceIndexCommandHandler> logger)
    : IRequestHandler<RebuildFaceIndexCommand, Result<int>>
{
    /// <inheritdoc />
    public async Task<Result<int>> Handle(
        RebuildFaceIndexCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            return Result.Failure<int>($"Event '{request.EventId}' was not found.");
        }

        if (!eventEntity.EnableFaceRecognition)
        {
            return Result.Failure<int>("Face recognition is not enabled for this event.");
        }

        // When not forcing a full rebuild, skip photos that already have embeddings.
        // A single DISTINCT query is far cheaper than re-running Python inference on every photo.
        HashSet<Guid>? alreadyIndexed = null;
        if (!request.Force)
        {
            alreadyIndexed = await embeddingRepository.GetIndexedPhotoIdsAsync(
                request.EventId, cancellationToken);
            logger.LogInformation(
                "Rebuild (smart mode): {Indexed} photo(s) already indexed will be skipped.",
                alreadyIndexed.Count);
        }

        const int batchSize = 200;
        var page = 1;
        var totalQueued = 0;
        var totalSkipped = 0;

        while (true)
        {
            var photos = await photoRepository.GetByEventIdAsync(
                request.EventId, page, batchSize, cancellationToken);

            if (photos.Count == 0)
                break;

            foreach (var photo in photos)
            {
                // Skip photos with existing embeddings unless a full force-rebuild was requested
                if (alreadyIndexed is not null && alreadyIndexed.Contains(photo.Id))
                {
                    totalSkipped++;
                    continue;
                }

                photo.QueueForFaceIndexing();
                await photoRepository.UpdateAsync(photo, cancellationToken);
                totalQueued++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (photos.Count < batchSize)
                break;

            page++;
        }

        logger.LogInformation(
            "Rebuild face index: {Queued} photo(s) queued, {Skipped} already-indexed skipped (Force={Force}) for event {EventId}.",
            totalQueued, totalSkipped, request.Force, request.EventId);

        return Result.Success(totalQueued);
    }
}
