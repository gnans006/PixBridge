using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.FaceSearch.Commands;

/// <summary>
/// Processes face detection and embedding storage for a single photo.
/// Dispatched by <see cref="Worker.Services.FaceIndexing.FaceIndexingService"/> for each photo in a batch.
/// </summary>
public sealed record ProcessFaceIndexCommand(Guid PhotoId) : IRequest<Result>;

/// <summary>Handles <see cref="ProcessFaceIndexCommand"/>.</summary>
public sealed class ProcessFaceIndexCommandHandler(
    IPhotoRepository photoRepository,
    IFaceEmbeddingRepository faceEmbeddingRepository,
    IFaceRecognitionService faceRecognitionService,
    IFaceNotificationService faceNotificationService,
    IUnitOfWork unitOfWork,
    ILogger<ProcessFaceIndexCommandHandler> logger)
    : IRequestHandler<ProcessFaceIndexCommand, Result>
{
    public async Task<Result> Handle(ProcessFaceIndexCommand request, CancellationToken cancellationToken)
    {
        var photo = await photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null)
            return Result.Failure($"Photo '{request.PhotoId}' not found.");

        // Transition status → Processing
        photo.MarkFaceIndexProcessing();
        await photoRepository.UpdateAsync(photo, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await faceRecognitionService.IndexPhotoAsync(photo.OriginalPath, cancellationToken);

            if (result.FaceCount > 0)
            {
                var embeddings = result.Faces.Select(f =>
                    FaceEmbedding.Create(photo.EventId, photo.Id, f.Embedding, f.BoundingBox, f.Confidence));

                await faceEmbeddingRepository.AddRangeAsync(embeddings, cancellationToken);
            }

            photo.MarkFaceIndexCompleted(result.FaceCount);
            await photoRepository.UpdateAsync(photo, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await faceNotificationService.NotifyFaceIndexCompletedAsync(
                photo.EventId, photo.Id, result.FaceCount, cancellationToken);

            logger.LogInformation(
                "Face indexing completed for photo {PhotoId}: {FaceCount} face(s) detected.",
                photo.Id, result.FaceCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Face indexing failed for photo {PhotoId} (attempt {Attempt}).",
                photo.Id, photo.FaceIndexRetryCount + 1);

            photo.MarkFaceIndexFailed();
            await photoRepository.UpdateAsync(photo, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure(ex.Message);
        }
    }
}
