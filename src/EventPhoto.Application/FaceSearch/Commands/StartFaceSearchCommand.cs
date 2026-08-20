using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Contracts.Responses.FaceSearch;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace EventPhoto.Application.FaceSearch.Commands;

/// <summary>
/// Creates a new <see cref="GuestFaceSession"/> from a guest's selfie bytes, then
/// immediately triggers the vector similarity search against the event's HNSW index.
/// Records analytics upon completion and uses selfie hash caching for repeated searches.
/// </summary>
public sealed record StartFaceSearchCommand(
    Guid EventId,
    byte[] SelfieBytes,
    float? ThresholdOverride = null) : IRequest<Result<FaceSearchStatusResponse>>, IRequiresFeature
{
    /// <inheritdoc />
    public string FeatureKey => Common.Models.FeatureKey.FaceSearchSessions;
}

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
    IAiSearchAnalyticsRepository analyticsRepository,
    IFaceRecognitionService faceRecognitionService,
    IFaceNotificationService faceNotificationService,
    IUnitOfWork unitOfWork,
    ILogger<StartFaceSearchCommandHandler> logger)
    : IRequestHandler<StartFaceSearchCommand, Result<FaceSearchStatusResponse>>
{
    private const string EmbeddingVersion = "arcface-512-v1";

    public async Task<Result<FaceSearchStatusResponse>> Handle(
        StartFaceSearchCommand request,
        CancellationToken cancellationToken)
    {
        var searchStart = DateTimeOffset.UtcNow;

        var eventEntity = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
            return Result.Failure<FaceSearchStatusResponse>($"Event '{request.EventId}' not found.");

        if (!eventEntity.IsActive)
            return Result.Failure<FaceSearchStatusResponse>("Event is not active.");

        if (!eventEntity.EnableFaceRecognition)
            return Result.Failure<FaceSearchStatusResponse>("Face recognition is not enabled for this event.");

        if (!eventEntity.AllowFaceSearch)
            return Result.Failure<FaceSearchStatusResponse>("Face search is not allowed for this event.");

        // ── Compute selfie hash for caching ───────────────────────────────────
        var selfieHash = Convert.ToHexString(
            SHA256.HashData(request.SelfieBytes)).ToLowerInvariant();

        // Precheck the selfie
        EmbeddingResult? embeddingResult = null;
        var usedFallback = false;
        var fallbackMessage = (string?)null;

        var precheck = await faceRecognitionService.PrecheckSelfieAsync(request.SelfieBytes, cancellationToken);
        if (!precheck.IsValid)
        {
            logger.LogWarning("Selfie precheck failed for event {EventId}: {Reason}", request.EventId, precheck.Message);
            usedFallback = true;
            fallbackMessage = precheck.Message ?? "We could not validate your selfie. Search completed with no matches.";
        }
        else
        {
            try
            {
                embeddingResult = await faceRecognitionService.GenerateEmbeddingAsync(
                    request.SelfieBytes, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Face recognition embedding generation failed for selfie search (EventId={EventId})", request.EventId);
                usedFallback = true;
                // Distinguish between "no face in selfie" (user error) and service being down
                fallbackMessage = (ex is InvalidOperationException && ex.Message.Contains("face", StringComparison.OrdinalIgnoreCase))
                    ? ex.Message
                    : "Face recognition service is unavailable right now. Please try again later.";
            }
        }

        var embedding = embeddingResult?.Embedding ?? Enumerable.Repeat(0f, 512).ToArray();

        // Create session with selfie hash
        var session = GuestFaceSession.Create(request.EventId, embedding, selfieHash);
        await sessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await faceNotificationService.NotifySearchStartedAsync(
            session.SessionToken, request.EventId, cancellationToken);

        session.MarkSearching();
        await sessionRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var threshold = request.ThresholdOverride ?? eventEntity.FaceMatchThreshold;

        List<PhotoMatch> photoMatches;
        float? topSimilarity = null;

        if (usedFallback)
        {
            photoMatches = [];
            await faceNotificationService.NotifySearchProgressAsync(
                session.SessionToken, 0, cancellationToken);
        }
        else
        {
            var hits = await embeddingRepository.SearchByEmbeddingAsync(
                request.EventId,
                embedding,
                threshold,
                topK: 200,
                cancellationToken);

            await faceNotificationService.NotifySearchProgressAsync(
                session.SessionToken, hits.Count, cancellationToken);

            // Deduplicate: keep the best similarity score per photo
            photoMatches = hits
                .GroupBy(h => h.PhotoId)
                .Select(g => PhotoMatch.Create(session.Id, g.Key, g.Max(x => x.Similarity)))
                .ToList();

            topSimilarity = photoMatches.Count > 0
                ? photoMatches.Max(m => m.SimilarityScore)
                : null;

            // If no matches at the event's configured threshold, fall back to a lenient 0.30 search.
            // This handles events created with the old default threshold of 0.75 and still surfaces
            // the best available matches rather than returning empty results.
            if (photoMatches.Count == 0 && threshold > 0.35f)
            {
                const float FallbackThreshold = 0.30f;
                var fallbackHits = await embeddingRepository.SearchByEmbeddingAsync(
                    request.EventId, embedding, FallbackThreshold, topK: 50, cancellationToken);

                if (fallbackHits.Count > 0)
                {
                    photoMatches = fallbackHits
                        .GroupBy(h => h.PhotoId)
                        .Select(g => PhotoMatch.Create(session.Id, g.Key, g.Max(x => x.Similarity)))
                        .ToList();
                    topSimilarity = photoMatches.Max(m => m.SimilarityScore);
                    logger.LogInformation(
                        "Find My Photos™ fallback search at {Threshold:P0} found {Count} photos for session {Token}.",
                        FallbackThreshold, photoMatches.Count, session.SessionToken);
                }
            }
        }

        if (photoMatches.Count > 0)
            await matchRepository.AddRangeAsync(photoMatches, cancellationToken);

        session.MarkCompleted(photoMatches.Count);
        await sessionRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Record analytics ───────────────────────────────────────────────────
        var searchDurationMs = (int)(DateTimeOffset.UtcNow - searchStart).TotalMilliseconds;
        var analyticsRecord = AiSearchAnalytics.Record(
            request.EventId,
            session.Id,
            photoMatches.Count,
            searchDurationMs,
            topSimilarity,
            EmbeddingVersion);
        await analyticsRepository.AddAsync(analyticsRecord, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await faceNotificationService.NotifySearchCompletedAsync(
            session.SessionToken, photoMatches.Count, session.ExpiresAt, cancellationToken);

        logger.LogInformation(
            "Find My Photos™ search completed for session {Token}: {MatchCount} photos in {DurationMs}ms.",
            session.SessionToken, photoMatches.Count, searchDurationMs);

        return Result.Success(new FaceSearchStatusResponse(
            session.SessionToken,
            session.Status.ToString(),
            session.MatchCount,
            session.ExpiresAt,
            fallbackMessage));
    }
}
