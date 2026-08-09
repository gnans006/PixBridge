using EventPhoto.Contracts.Responses.AiDiscovery;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.AiDiscovery.Queries;

/// <summary>Returns the paged processing queue for the AI Studio queue panel.</summary>
public sealed record GetProcessingQueueQuery(
    int Page = 1,
    int PageSize = 25,
    FaceJobStatus? StatusFilter = null) : IRequest<Result<ProcessingQueueResponse>>;

/// <summary>Handles <see cref="GetProcessingQueueQuery"/>.</summary>
public sealed class GetProcessingQueueQueryHandler(
    IFaceProcessingJobRepository jobRepository,
    IEventRepository eventRepository,
    IPhotoRepository photoRepository)
    : IRequestHandler<GetProcessingQueueQuery, Result<ProcessingQueueResponse>>
{
    public async Task<Result<ProcessingQueueResponse>> Handle(
        GetProcessingQueueQuery request,
        CancellationToken cancellationToken)
    {
        var statusCounts = await jobRepository.GetStatusCountsAsync(cancellationToken);

        statusCounts.TryGetValue(FaceJobStatus.Pending, out var pendingCount);
        statusCounts.TryGetValue(FaceJobStatus.Queued, out var queuedCount);
        var processingCount = statusCounts.Where(kv =>
            kv.Key is FaceJobStatus.Detecting or FaceJobStatus.QualityChecking
                   or FaceJobStatus.Embedding or FaceJobStatus.Indexing)
            .Sum(kv => kv.Value);
        statusCounts.TryGetValue(FaceJobStatus.Failed, out var failedCount);

        var (jobs, total) = await jobRepository.GetDeadLetteredPagedAsync(
            request.Page, request.PageSize, cancellationToken);

        // Load related data for display
        var eventIds = jobs.Select(j => j.EventId).Distinct().ToList();
        var photoIds = jobs.Select(j => j.PhotoId).Distinct().ToList();

        var events = (await Task.WhenAll(
            eventIds.Select(id => eventRepository.GetByIdAsync(id, cancellationToken))))
            .Where(e => e is not null)
            .ToDictionary(e => e!.Id, e => e!.Name);

        var photos = (await photoRepository.GetByIdsAsync(photoIds, cancellationToken))
            .ToDictionary(p => p.Id);

        var items = jobs.Select(job => new ProcessingQueueItemResponse(
            JobId: job.Id,
            EventId: job.EventId,
            EventName: events.GetValueOrDefault(job.EventId, "Unknown Event"),
            PhotoId: job.PhotoId,
            FileName: photos.TryGetValue(job.PhotoId, out var ph) ? ph.FileName : "Unknown",
            Status: job.Status.ToString(),
            RetryCount: job.RetryCount,
            CreatedAt: job.CreatedAt,
            StartedAt: job.StartedAt,
            NextRetryAt: job.NextRetryAt)).ToList();

        return Result.Success(new ProcessingQueueResponse(
            Items: items,
            TotalCount: total,
            Page: request.Page,
            PageSize: request.PageSize,
            HasNextPage: request.Page * request.PageSize < total,
            PendingCount: pendingCount + queuedCount,
            ProcessingCount: processingCount,
            FailedCount: failedCount));
    }
}

/// <summary>Returns the paged dead-letter queue for the AI Studio failed-jobs panel.</summary>
public sealed record GetDeadLetterQueueQuery(
    int Page = 1,
    int PageSize = 25) : IRequest<Result<DeadLetterQueueResponse>>;

/// <summary>Handles <see cref="GetDeadLetterQueueQuery"/>.</summary>
public sealed class GetDeadLetterQueueQueryHandler(
    IFaceProcessingJobRepository jobRepository,
    IEventRepository eventRepository,
    IPhotoRepository photoRepository,
    ISystemSettingRepository settingRepository)
    : IRequestHandler<GetDeadLetterQueueQuery, Result<DeadLetterQueueResponse>>
{
    public async Task<Result<DeadLetterQueueResponse>> Handle(
        GetDeadLetterQueueQuery request,
        CancellationToken cancellationToken)
    {
        var (jobs, total) = await jobRepository.GetDeadLetteredPagedAsync(
            request.Page, request.PageSize, cancellationToken);

        var eventIds = jobs.Select(j => j.EventId).Distinct().ToList();
        var photoIds = jobs.Select(j => j.PhotoId).Distinct().ToList();

        var events = (await Task.WhenAll(
            eventIds.Select(id => eventRepository.GetByIdAsync(id, cancellationToken))))
            .Where(e => e is not null)
            .ToDictionary(e => e!.Id, e => e!.Name);

        var photos = (await photoRepository.GetByIdsAsync(photoIds, cancellationToken))
            .ToDictionary(p => p.Id);

        var serverUrl = await settingRepository.GetValueAsync("app.serverUrl", cancellationToken)
            ?? "http://localhost:5000";

        var items = jobs.Select(job =>
        {
            var photoExists = photos.TryGetValue(job.PhotoId, out var ph);
            var thumbUrl = photoExists
                ? $"{serverUrl.TrimEnd('/')}/api/photos/{job.PhotoId}/thumbnail"
                : string.Empty;

            return new DeadLetterJobResponse(
                JobId: job.Id,
                EventId: job.EventId,
                EventName: events.GetValueOrDefault(job.EventId, "Unknown Event"),
                PhotoId: job.PhotoId,
                FileName: photoExists ? ph!.FileName : "Unknown",
                ThumbnailUrl: thumbUrl,
                Status: job.Status.ToString(),
                FailureType: job.FailureType?.ToString(),
                LastError: job.LastError,
                RetryCount: job.RetryCount,
                CreatedAt: job.CreatedAt,
                CompletedAt: job.CompletedAt);
        }).ToList();

        return Result.Success(new DeadLetterQueueResponse(
            Items: items,
            TotalCount: total,
            Page: request.Page,
            PageSize: request.PageSize,
            HasNextPage: request.Page * request.PageSize < total));
    }
}
