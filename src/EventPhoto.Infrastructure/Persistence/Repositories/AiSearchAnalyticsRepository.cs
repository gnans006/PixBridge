using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>PostgreSQL implementation of <see cref="IAiSearchAnalyticsRepository"/>.</summary>
public sealed class AiSearchAnalyticsRepository(AppDbContext context) : IAiSearchAnalyticsRepository
{
    /// <inheritdoc />
    public async Task AddAsync(AiSearchAnalytics analytics, CancellationToken cancellationToken = default)
        => await context.AiSearchAnalytics.AddAsync(analytics, cancellationToken);

    /// <inheritdoc />
    public async Task<AiSearchAggregates> GetAggregatesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.AiSearchAnalytics
            .Where(a => a.SearchedAt >= from && a.SearchedAt <= to)
            .Select(a => new { a.WasSuccessful, a.SearchDurationMs, a.MatchesFound })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return new AiSearchAggregates(0, 0, 0, 0, 0);

        var total = rows.Count;
        var successful = rows.Count(r => r.WasSuccessful);
        return new AiSearchAggregates(
            TotalSearches: total,
            SuccessfulSearches: successful,
            SuccessRatePercent: total > 0 ? (double)successful / total * 100.0 : 0,
            AverageSearchDurationMs: rows.Average(r => r.SearchDurationMs),
            AverageMatchesFound: rows.Average(r => r.MatchesFound));
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, AiSearchAggregates>> GetEventAggregatesAsync(
        IEnumerable<Guid> eventIds,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var ids = eventIds.ToList();
        var rows = await context.AiSearchAnalytics
            .Where(a => ids.Contains(a.EventId) && a.SearchedAt >= from && a.SearchedAt <= to)
            .Select(a => new { a.EventId, a.WasSuccessful, a.SearchDurationMs, a.MatchesFound })
            .ToListAsync(cancellationToken);

        return rows.GroupBy(r => r.EventId).ToDictionary(
            g => g.Key,
            g =>
            {
                var total = g.Count();
                var successful = g.Count(r => r.WasSuccessful);
                return new AiSearchAggregates(
                    TotalSearches: total,
                    SuccessfulSearches: successful,
                    SuccessRatePercent: total > 0 ? (double)successful / total * 100.0 : 0,
                    AverageSearchDurationMs: g.Average(r => r.SearchDurationMs),
                    AverageMatchesFound: g.Average(r => r.MatchesFound));
            });
    }

    /// <inheritdoc />
    public async Task<List<(Guid EventId, int SearchCount)>> GetTopEventsByVolumeAsync(
        int topN,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.AiSearchAnalytics
            .Where(a => a.SearchedAt >= from && a.SearchedAt <= to)
            .GroupBy(a => a.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.EventId, r.Count)).ToList();
    }
}
