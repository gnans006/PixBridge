using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.AiDiscovery.Commands;

/// <summary>
/// Executes the complete AI Discovery Pipeline for a single photo.
/// Orchestrates: detect → quality-check → embed → index → complete.
///
/// <para>This command is dispatched by <c>AiDiscoveryPipelineService</c> for each job
/// dequeued from the priority channel.</para>
///
/// <para>Failure types are classified to determine retry vs permanent failure routing.</para>
/// </summary>
public sealed record ProcessAiDiscoveryJobCommand(Guid JobId) : IRequest<Result>;

/// <summary>Handles <see cref="ProcessAiDiscoveryJobCommand"/>.</summary>
public sealed class ProcessAiDiscoveryJobCommandHandler(
    IFaceProcessingJobRepository jobRepository,
    IPhotoRepository photoRepository,
    IFaceEmbeddingRepository embeddingRepository,
    IFaceRecognitionService faceRecognitionService,
    IFaceQualityService qualityService,
    IFaceNotificationService faceNotificationService,
    IUnitOfWork unitOfWork,
    ILogger<ProcessAiDiscoveryJobCommandHandler> logger)
    : IRequestHandler<ProcessAiDiscoveryJobCommand, Result>
{
    private const string EmbeddingVersion = "arcface-512-v1";

    public async Task<Result> Handle(
        ProcessAiDiscoveryJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job is null)
            return Result.Failure($"FaceProcessingJob '{request.JobId}' not found.");

        var photo = await photoRepository.GetByIdAsync(job.PhotoId, cancellationToken);
        if (photo is null)
        {
            job.MarkFailed("Photo not found in database.", FaceFailureType.StorageUnavailable);
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure("Photo not found.");
        }

        if (!System.IO.File.Exists(photo.OriginalPath))
        {
            job.MarkFailed(
                $"Original photo file not found at '{photo.OriginalPath}'.",
                FaceFailureType.StorageUnavailable);
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure("Photo file not on disk.");
        }

        // ── Step 1: Face Detection ─────────────────────────────────────────────
        job.MarkDetecting();
        await jobRepository.UpdateAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        IndexPhotoResult detectionResult;
        try
        {
            detectionResult = await faceRecognitionService.IndexPhotoAsync(
                photo.OriginalPath, cancellationToken);
        }
        catch (Exception ex) when (IsCorruptImageException(ex))
        {
            logger.LogWarning(ex, "Corrupt image detected for photo {PhotoId}.", photo.Id);
            job.MarkFailed($"Image is corrupt or unreadable: {ex.Message}", FaceFailureType.CorruptedImage);
            await FinalizeJobAsync(job, photo, 0, cancellationToken);
            return Result.Success(); // not a system failure
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Embedding service failure for photo {PhotoId}.", photo.Id);
            job.MarkFailed($"Embedding service error: {ex.Message}", FaceFailureType.EmbeddingServiceFailure);
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(ex.Message);
        }

        // ── No Face Detected — not a failure ───────────────────────────────────
        if (detectionResult.FaceCount == 0)
        {
            logger.LogInformation(
                "No face detected in photo {PhotoId} — completing without embeddings.", photo.Id);
            job.MarkCompleted();
            await FinalizeJobAsync(job, photo, 0, cancellationToken);
            await faceNotificationService.NotifyFaceIndexCompletedAsync(
                photo.EventId, photo.Id, 0, cancellationToken);
            return Result.Success();
        }

        // ── Step 2: Quality Filtering ───────────────────────────────────────────
        job.MarkQualityChecking();
        await jobRepository.UpdateAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var imageTotalPixels = (photo.Width ?? 1920) * (photo.Height ?? 1080);
        var qualifiedFaces = new List<(FaceDetectionResult Face, FaceQualityResult Quality)>();

        foreach (var face in detectionResult.Faces)
        {
            var quality = qualityService.Evaluate(
                face.Confidence,
                face.BoundingBox,
                imageTotalPixels,
                face.PoseAngles);

            // Only reject faces with virtually no usable signal (score < 10 means detection confidence < 0.2)
            if (quality.Score < 10f)
            {
                logger.LogDebug(
                    "Face skipped (quality {Score}/100, {Reason}) in photo {PhotoId}.",
                    quality.Score, quality.RejectionReason, photo.Id);
                continue;
            }

            qualifiedFaces.Add((face, quality));
        }

        if (qualifiedFaces.Count == 0)
        {
            logger.LogInformation(
                "All {Count} face(s) in photo {PhotoId} failed quality check — completing without embeddings.",
                detectionResult.FaceCount, photo.Id);
            job.MarkCompleted();
            await FinalizeJobAsync(job, photo, 0, cancellationToken);
            return Result.Success();
        }

        // ── Step 3: Embedding + Indexing ───────────────────────────────────────
        job.MarkEmbedding();
        await jobRepository.UpdateAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var embeddings = qualifiedFaces.Select(qf => FaceEmbedding.Create(
            photo.EventId,
            photo.Id,
            qf.Face.Embedding,
            qf.Face.BoundingBox,
            qf.Face.Confidence,
            qf.Quality.Score,
            qf.Quality.Tier,
            detectionResult.FaceCount,
            EmbeddingVersion));

        job.MarkIndexing();
        await jobRepository.UpdateAsync(job, cancellationToken);

        try
        {
            await embeddingRepository.AddRangeAsync(embeddings, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database failure writing embeddings for photo {PhotoId}.", photo.Id);
            job.MarkFailed($"Database error: {ex.Message}", FaceFailureType.DatabaseUnavailable);
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(ex.Message);
        }

        // ── Step 4: Complete ───────────────────────────────────────────────────
        job.MarkCompleted();
        await FinalizeJobAsync(job, photo, qualifiedFaces.Count, cancellationToken);

        await faceNotificationService.NotifyFaceIndexCompletedAsync(
            photo.EventId, photo.Id, qualifiedFaces.Count, cancellationToken);

        logger.LogInformation(
            "AI Discovery completed for photo {PhotoId}: {Indexed}/{Detected} face(s) indexed.",
            photo.Id, qualifiedFaces.Count, detectionResult.FaceCount);

        return Result.Success();
    }

    private async Task FinalizeJobAsync(
        FaceProcessingJob job,
        Photo photo,
        int faceCount,
        CancellationToken cancellationToken)
    {
        photo.MarkFaceIndexCompleted(faceCount);
        await jobRepository.UpdateAsync(job, cancellationToken);
        await photoRepository.UpdateAsync(photo, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static bool IsCorruptImageException(Exception ex)
        => ex.Message.Contains("corrupt", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("Could not decode", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("Invalid image", StringComparison.OrdinalIgnoreCase);
}
