using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.AiDiscovery.Commands;

/// <summary>
/// Creates a <see cref="FaceProcessingJob"/> for a photo that has just been thumbnail-processed
/// and entered the AI Discovery Pipeline.
///
/// <para>This command is dispatched by <c>ThumbnailProcessorService</c> after thumbnail
/// generation succeeds, ensuring the Gallery Pipeline and AI Pipeline remain fully independent.
/// The photo is gallery-visible before this command runs.</para>
/// </summary>
public sealed record EnqueuePhotoForAiDiscoveryCommand(
    Guid EventId,
    Guid PhotoId,
    int Priority = 2) : IRequest<Result>;

/// <summary>Handles <see cref="EnqueuePhotoForAiDiscoveryCommand"/>.</summary>
public sealed class EnqueuePhotoForAiDiscoveryCommandHandler(
    IFaceProcessingJobRepository jobRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    ILogger<EnqueuePhotoForAiDiscoveryCommandHandler> logger)
    : IRequestHandler<EnqueuePhotoForAiDiscoveryCommand, Result>
{
    public async Task<Result> Handle(
        EnqueuePhotoForAiDiscoveryCommand request,
        CancellationToken cancellationToken)
    {
        // Only enqueue when the event has AI Discovery enabled
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
            return Result.Failure($"Event '{request.EventId}' not found.");

        if (!eventEntity.EnableFaceRecognition)
        {
            logger.LogDebug(
                "Skipping AI enqueue for photo {PhotoId} — event {EventId} has face recognition disabled.",
                request.PhotoId, request.EventId);
            return Result.Success();
        }

        // Idempotent — skip if an active job already exists for this photo
        var existing = await jobRepository.GetActiveByPhotoIdAsync(request.PhotoId, cancellationToken);
        if (existing is not null)
        {
            logger.LogDebug(
                "AI job already exists for photo {PhotoId} (status={Status}).",
                request.PhotoId, existing.Status);
            return Result.Success();
        }

        var job = FaceProcessingJob.Create(request.EventId, request.PhotoId, request.Priority);
        await jobRepository.AddAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Photo {PhotoId} enqueued for AI Discovery (event {EventId}, priority {Priority}).",
            request.PhotoId, request.EventId, request.Priority);

        return Result.Success();
    }
}
