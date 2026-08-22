using EventPhoto.Contracts.Responses.AiDiscovery;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.AiDiscovery.Queries;

/// <summary>Returns per-event AI health metrics for the AI Studio event health panel.</summary>
public sealed record GetEventAiHealthQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<IReadOnlyList<EventAiHealthResponse>>>;

/// <summary>Handles <see cref="GetEventAiHealthQuery"/>.</summary>
public sealed class GetEventAiHealthQueryHandler(
    IEventRepository eventRepository,
    IFaceEmbeddingRepository embeddingRepository,
    IFaceProcessingJobRepository jobRepository,
    IAiSearchAnalyticsRepository analyticsRepository)
    : IRequestHandler<GetEventAiHealthQuery, Result<IReadOnlyList<EventAiHealthResponse>>>
{
    public async Task<Result<IReadOnlyList<EventAiHealthResponse>>> Handle(
        GetEventAiHealthQuery request,
        CancellationToken cancellationToken)
    {
        var events = await eventRepository.GetAllActiveAsync(cancellationToken);
        var eventIds = events.Select(e => e.Id).ToList();

        var jobCounts = await jobRepository.GetEventJobCountsAsync(eventIds, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var analyticsWindow = now.AddDays(-7);
        var eventAnalytics = await analyticsRepository.GetEventAggregatesAsync(
            eventIds, analyticsWindow, now, cancellationToken);

        var results = new List<EventAiHealthResponse>();

        foreach (var ev in events)
        {
            var facesIndexed = await embeddingRepository.CountByEventIdAsync(ev.Id, cancellationToken);

            jobCounts.TryGetValue(ev.Id, out var counts);
            var (pending, failed, completed, deadLettered) = counts;

            eventAnalytics.TryGetValue(ev.Id, out var analytics);

            var totalPhotos = ev.PhotoCount;
            var indexCompletion = totalPhotos > 0
                ? Math.Min(100.0, (completed / (double)totalPhotos) * 100.0)
                : 0.0;

            results.Add(new EventAiHealthResponse(
                EventId: ev.Id,
                EventName: ev.Name,
                TotalPhotos: totalPhotos,
                FacesIndexed: facesIndexed,
                PendingJobs: pending,
                FailedJobs: failed,
                DeadLetteredJobs: deadLettered,
                IndexCompletionPercent: indexCompletion,
                AverageSearchDurationMs: analytics?.AverageSearchDurationMs ?? 0,
                SearchSuccessRatePercent: analytics?.SuccessRatePercent ?? 0,
                TotalSearches: analytics?.TotalSearches ?? 0,
                IsIndexComplete: indexCompletion >= 99.0));
        }

        return Result.Success<IReadOnlyList<EventAiHealthResponse>>(results);
    }
}

/// <summary>Returns AI search analytics for the studio analytics panel.</summary>
public sealed record GetAiAnalyticsQuery(
    int WindowHours = 24) : IRequest<Result<AiAnalyticsResponse>>;

/// <summary>Handles <see cref="GetAiAnalyticsQuery"/>.</summary>
public sealed class GetAiAnalyticsQueryHandler(
    IAiSearchAnalyticsRepository analyticsRepository,
    IEventRepository eventRepository)
    : IRequestHandler<GetAiAnalyticsQuery, Result<AiAnalyticsResponse>>
{
    public async Task<Result<AiAnalyticsResponse>> Handle(
        GetAiAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var from = now.AddHours(-request.WindowHours);

        var aggregates = await analyticsRepository.GetAggregatesAsync(from, now, cancellationToken);
        var topEvents = await analyticsRepository.GetTopEventsByVolumeAsync(10, from, now, cancellationToken);

        // Load event names for top events
        var eventIds = topEvents.Select(t => t.EventId).ToList();
        var eventNames = (await Task.WhenAll(
            eventIds.Select(id => eventRepository.GetByIdAsync(id, cancellationToken))))
            .Where(e => e is not null)
            .ToDictionary(e => e!.Id, e => e!.Name);

        var eventAnalytics = await analyticsRepository.GetEventAggregatesAsync(
            eventIds, from, now, cancellationToken);

        var topEventItems = topEvents.Select(t => new TopEventAnalyticsItem(
            EventId: t.EventId,
            EventName: eventNames.GetValueOrDefault(t.EventId, "Unknown Event"),
            SearchCount: t.SearchCount,
            SuccessRatePercent: eventAnalytics.TryGetValue(t.EventId, out var ea)
                ? ea.SuccessRatePercent : 0)).ToList();

        return Result.Success(new AiAnalyticsResponse(
            TotalSearches: aggregates.TotalSearches,
            SuccessfulSearches: aggregates.SuccessfulSearches,
            SuccessRatePercent: aggregates.SuccessRatePercent,
            AverageSearchDurationMs: aggregates.AverageSearchDurationMs,
            AverageMatchesFound: aggregates.AverageMatchesFound,
            TopEvents: topEventItems,
            HourlyVolume: [])); // populated by raw query in future
    }
}
