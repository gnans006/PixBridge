using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for <see cref="AiSearchAnalytics"/> persistence.</summary>
public interface IAiSearchAnalyticsRepository
{
    /// <summary>Persists a new analytics record.</summary>
    Task AddAsync(AiSearchAnalytics analytics, CancellationToken cancellationToken = default);

    /// <summary>Returns aggregate statistics for the AI Studio overview panel.</summary>
    Task<AiSearchAggregates> GetAggregatesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>Returns per-event aggregates for the event health panel.</summary>
    Task<Dictionary<Guid, AiSearchAggregates>> GetEventAggregatesAsync(
        IEnumerable<Guid> eventIds,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the top events by search volume in the given time window.</summary>
    Task<List<(Guid EventId, int SearchCount)>> GetTopEventsByVolumeAsync(
        int topN,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}

/// <summary>Aggregate statistics computed over a set of <see cref="AiSearchAnalytics"/> records.</summary>
public sealed record AiSearchAggregates(
    int TotalSearches,
    int SuccessfulSearches,
    double SuccessRatePercent,
    double AverageSearchDurationMs,
    double AverageMatchesFound);
