using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.FaceSearch;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.FaceSearch.Commands;

/// <summary>
/// Creates a new <see cref="GuestFaceSession"/> from a guest's selfie bytes, then
/// immediately triggers the vector similarity search against the event's HNSW index.
/// </summary>
public sealed record StartFaceSearchCommand(
    Guid EventId,
    byte[] SelfieBytes,
    float? ThresholdOverride = null) : IRequest<Result<FaceSearchStatusResponse>>;

/// <summary>Validates <see cref="StartFaceSearchCommand"/>.</summary>
public sealed class StartFaceSearchCommandValidator : AbstractValidator<StartFaceSearchCommand>
{
    public StartFaceSearchCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty().WithMessage("EventId is required.");
        RuleFor(x => x.SelfieBytes)
            .NotNull().WithMessage("Selfie image is required.")
            .Must(b => b.Length > 0).WithMessage("Selfie image cannot be empty.")
            .Must(b => b.Length <= 10 * 1024 * 1024).WithMessage("Selfie image must not exceed 10 MB.");
        RuleFor(x => x.ThresholdOverride)
            .InclusiveBetween(0.0f, 1.0f).When(x => x.ThresholdOverride.HasValue)
            .WithMessage("Threshold must be between 0.0 and 1.0.");
    }
}

/// <summary>Handles <see cref="StartFaceSearchCommand"/>.</summary>
public sealed class StartFaceSearchCommandHandler(
    IEventRepository eventRepository,
    IGuestFaceSessionRepository sessionRepository,
    IFaceEmbeddingRepository embeddingRepository,
    IPhotoMatchRepository matchRepository,
    IFaceRecognitionService faceRecognitionService,
    IFaceNotificationService faceNotificationService,
    IUnitOfWork unitOfWork,
    ILogger<StartFaceSearchCommandHandler> logger)
    : IRequestHandler<StartFaceSearchCommand, Result<FaceSearchStatusResponse>>
{
    public async Task<Result<FaceSearchStatusResponse>> Handle(
        StartFaceSearchCommand request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
            return Result.Failure<FaceSearchStatusResponse>($"Event '{request.EventId}' not found.");

        if (!eventEntity.IsActive)
            return Result.Failure<FaceSearchStatusResponse>("Event is not active.");

        if (!eventEntity.EnableFaceRecognition)
            return Result.Failure<FaceSearchStatusResponse>("Face recognition is not enabled for this event.");

        if (!eventEntity.AllowFaceSearch)
            return Result.Failure<FaceSearchStatusResponse>("Face search is not allowed for this event.");

        // Generate embedding from selfie
        EmbeddingResult embeddingResult;
        try
        {
            embeddingResult = await faceRecognitionService.GenerateEmbeddingAsync(
                request.SelfieBytes, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate embedding for selfie (EventId={EventId})", request.EventId);
            return Result.Failure<FaceSearchStatusResponse>(
                "Could not process your selfie. Please ensure your face is clearly visible and try again.");
        }

        // Create session
        var session = GuestFaceSession.Create(request.EventId, embeddingResult.Embedding);
        await sessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await faceNotificationService.NotifySearchStartedAsync(
            session.SessionToken, request.EventId, cancellationToken);

        // Transition → Searching
        session.MarkSearching();
        await sessionRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var threshold = request.ThresholdOverride ?? eventEntity.FaceMatchThreshold;

        // pgvector HNSW nearest-neighbour search
        var hits = await embeddingRepository.SearchByEmbeddingAsync(
            request.EventId,
            embeddingResult.Embedding,
            threshold,
            topK: 200,
            cancellationToken);

        await faceNotificationService.NotifySearchProgressAsync(
            session.SessionToken, hits.Count, cancellationToken);

        // Deduplicate: keep the best similarity score per photo
        var photoMatches = hits
            .GroupBy(h => h.PhotoId)
            .Select(g => PhotoMatch.Create(session.Id, g.Key, g.Max(x => x.Similarity)))
            .ToList();

        if (photoMatches.Count > 0)
            await matchRepository.AddRangeAsync(photoMatches, cancellationToken);

        session.MarkCompleted(photoMatches.Count);
        await sessionRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await faceNotificationService.NotifySearchCompletedAsync(
            session.SessionToken, photoMatches.Count, session.ExpiresAt, cancellationToken);

        logger.LogInformation(
            "Face search completed for session {Token}: {MatchCount} photos matched.",
            session.SessionToken, photoMatches.Count);

        return Result.Success(new FaceSearchStatusResponse(
            session.SessionToken,
            session.Status.ToString(),
            session.MatchCount,
            session.ExpiresAt));
    }
}
