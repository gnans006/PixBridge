using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Events.Commands;

/// <summary>
/// Re-queues all photos of a face-recognition-enabled event for indexing.
/// The background worker will pick them up and process them asynchronously.
/// </summary>
/// <param name="EventId">The event to rebuild face index for.</param>
public sealed record RebuildFaceIndexCommand(Guid EventId) : IRequest<Result<int>>;

/// <summary>Handles <see cref="RebuildFaceIndexCommand"/>.</summary>
public sealed class RebuildFaceIndexCommandHandler(
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
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

        // Page through all photos and queue each for re-indexing.
        const int batchSize = 200;
        var page = 1;
        var totalQueued = 0;

        while (true)
        {
            var photos = await photoRepository.GetByEventIdAsync(
                request.EventId, page, batchSize, cancellationToken);

            if (photos.Count == 0)
            {
                break;
            }

            foreach (var photo in photos)
            {
                photo.QueueForFaceIndexing();
                await photoRepository.UpdateAsync(photo, cancellationToken);
                totalQueued++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (photos.Count < batchSize)
            {
                break;
            }

            page++;
        }

        logger.LogInformation(
            "Rebuild face index queued {Count} photos for event {EventId}.",
            totalQueued,
            request.EventId);

        return Result.Success(totalQueued);
    }
}
