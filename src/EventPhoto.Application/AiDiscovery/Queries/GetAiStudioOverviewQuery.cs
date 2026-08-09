using EventPhoto.Contracts.Responses.AiDiscovery;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.AiDiscovery.Queries;

/// <summary>Returns the aggregated overview metrics for the AI Studio header panel.</summary>
public sealed record GetAiStudioOverviewQuery : IRequest<Result<AiStudioOverviewResponse>>;

/// <summary>Handles <see cref="GetAiStudioOverviewQuery"/>.</summary>
public sealed class GetAiStudioOverviewQueryHandler(
    IFaceProcessingJobRepository jobRepository,
    IFaceEmbeddingRepository embeddingRepository,
    IAiSearchAnalyticsRepository analyticsRepository)
    : IRequestHandler<GetAiStudioOverviewQuery, Result<AiStudioOverviewResponse>>
{
    public async Task<Result<AiStudioOverviewResponse>> Handle(
        GetAiStudioOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var statusCounts = await jobRepository.GetStatusCountsAsync(cancellationToken);
        var queueDepth = await jobRepository.GetQueueDepthAsync(cancellationToken);

        // We use a rough count of distinct photos with embeddings as "indexed"
        // (TotalPhotosIndexed) and total embedding rows as "faces indexed"
        var totalFacesIndexed = await embeddingRepository.CountAllAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var analytics = await analyticsRepository.GetAggregatesAsync(
            now.AddHours(-24), now, cancellationToken);

        statusCounts.TryGetValue(FaceJobStatus.Pending, out var pending);
        statusCounts.TryGetValue(FaceJobStatus.Queued, out var queued);
        statusCounts.TryGetValue(FaceJobStatus.Detecting, out var detecting);
        statusCounts.TryGetValue(FaceJobStatus.QualityChecking, out var qualityChecking);
        statusCounts.TryGetValue(FaceJobStatus.Embedding, out var embedding);
        statusCounts.TryGetValue(FaceJobStatus.Indexing, out var indexing);
        statusCounts.TryGetValue(FaceJobStatus.Failed, out var failed);
        statusCounts.TryGetValue(FaceJobStatus.DeadLettered, out var deadLettered);
        statusCounts.TryGetValue(FaceJobStatus.Completed, out var completed);

        var processing = queued + detecting + qualityChecking + embedding + indexing;
        var totalPhotosIndexed = completed;
        var isPipelineHealthy = deadLettered == 0 && failed < 10;

        var statusMessage = isPipelineHealthy
            ? "Pipeline is healthy."
            : $"{deadLettered} job(s) in dead-letter queue. {failed} job(s) pending retry.";

        return Result.Success(new AiStudioOverviewResponse(
            TotalPhotosIndexed: totalPhotosIndexed,
            TotalFacesIndexed: totalFacesIndexed,
            PendingJobs: pending,
            ProcessingJobs: processing,
            FailedJobs: failed,
            DeadLetteredJobs: deadLettered,
            QueueDepth: queueDepth,
            AverageSearchDurationMs: analytics.AverageSearchDurationMs,
            SearchSuccessRatePercent: analytics.SuccessRatePercent,
            TotalSearchesLast24H: analytics.TotalSearches,
            IsPipelineHealthy: isPipelineHealthy,
            PipelineStatusMessage: statusMessage,
            GeneratedAt: now));
    }
}
