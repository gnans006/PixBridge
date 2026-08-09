namespace EventPhoto.Contracts.Responses.AiDiscovery;

/// <summary>Overview metrics for the AI Studio dashboard header panel.</summary>
public sealed record AiStudioOverviewResponse(
    int TotalPhotosIndexed,
    int TotalFacesIndexed,
    int PendingJobs,
    int ProcessingJobs,
    int FailedJobs,
    int DeadLetteredJobs,
    int QueueDepth,
    double AverageSearchDurationMs,
    double SearchSuccessRatePercent,
    int TotalSearchesLast24H,
    bool IsPipelineHealthy,
    string PipelineStatusMessage,
    DateTimeOffset GeneratedAt);

/// <summary>Summary of a single job in the processing queue.</summary>
public sealed record ProcessingQueueItemResponse(
    Guid JobId,
    Guid EventId,
    string EventName,
    Guid PhotoId,
    string FileName,
    string Status,
    int RetryCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? NextRetryAt);

/// <summary>Paged processing queue response.</summary>
public sealed record ProcessingQueueResponse(
    IReadOnlyList<ProcessingQueueItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasNextPage,
    int PendingCount,
    int ProcessingCount,
    int FailedCount);

/// <summary>A dead-lettered job with full failure audit trail.</summary>
public sealed record DeadLetterJobResponse(
    Guid JobId,
    Guid EventId,
    string EventName,
    Guid PhotoId,
    string FileName,
    string ThumbnailUrl,
    string Status,
    string? FailureType,
    string? LastError,
    int RetryCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>Paged dead-letter queue response.</summary>
public sealed record DeadLetterQueueResponse(
    IReadOnlyList<DeadLetterJobResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasNextPage);

/// <summary>AI health metrics for a single event.</summary>
public sealed record EventAiHealthResponse(
    Guid EventId,
    string EventName,
    int TotalPhotos,
    int FacesIndexed,
    int PendingJobs,
    int FailedJobs,
    int DeadLetteredJobs,
    double IndexCompletionPercent,
    double AverageSearchDurationMs,
    double SearchSuccessRatePercent,
    int TotalSearches,
    bool IsIndexComplete);

/// <summary>AI search analytics for the analytics panel.</summary>
public sealed record AiAnalyticsResponse(
    int TotalSearches,
    int SuccessfulSearches,
    double SuccessRatePercent,
    double AverageSearchDurationMs,
    double AverageMatchesFound,
    IReadOnlyList<TopEventAnalyticsItem> TopEvents,
    IReadOnlyList<HourlySearchVolumeItem> HourlyVolume);

/// <summary>Top event by search volume.</summary>
public sealed record TopEventAnalyticsItem(
    Guid EventId,
    string EventName,
    int SearchCount,
    double SuccessRatePercent);

/// <summary>Hourly search volume data point.</summary>
public sealed record HourlySearchVolumeItem(
    DateTimeOffset Hour,
    int SearchCount,
    int SuccessCount);
