using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Records a single guest face-search event for analytics and observability.
/// Written at the end of each search session to power the AI Studio dashboard.
/// </summary>
public sealed class AiSearchAnalytics : Entity
{
    private AiSearchAnalytics()
    {
    }

    /// <summary>Gets the parent event identifier.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Gets the guest face-search session identifier.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>Gets the number of photos matched in this search.</summary>
    public int MatchesFound { get; private set; }

    /// <summary>Gets the total end-to-end search duration in milliseconds.</summary>
    public int SearchDurationMs { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the search found at least one confident match.
    /// A search is considered successful when <see cref="MatchesFound"/> &gt; 0.
    /// </summary>
    public bool WasSuccessful { get; private set; }

    /// <summary>Gets the highest confidence category among matched photos, if any.</summary>
    public MatchConfidenceCategory? TopMatchCategory { get; private set; }

    /// <summary>Gets the raw cosine similarity of the best match (0.0–1.0).</summary>
    public float? TopSimilarityScore { get; private set; }

    /// <summary>Gets the embedding version used during this search (for model drift tracking).</summary>
    public string EmbeddingVersion { get; private set; } = "arcface-512-v1";

    /// <summary>Gets when this analytics record was created.</summary>
    public DateTimeOffset SearchedAt { get; private set; }

    /// <summary>Gets the event navigation property.</summary>
    public Event? Event { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new analytics record at the completion of a face-search session.
    /// </summary>
    public static AiSearchAnalytics Record(
        Guid eventId,
        Guid sessionId,
        int matchesFound,
        int searchDurationMs,
        float? topSimilarityScore,
        string embeddingVersion = "arcface-512-v1")
    {
        if (eventId == Guid.Empty)
            throw new DomainException("EventId is required.");
        if (sessionId == Guid.Empty)
            throw new DomainException("SessionId is required.");
        if (searchDurationMs < 0)
            throw new DomainException("SearchDurationMs cannot be negative.");

        MatchConfidenceCategory? topCategory = topSimilarityScore switch
        {
            >= 0.90f => MatchConfidenceCategory.Excellent,
            >= 0.80f => MatchConfidenceCategory.Strong,
            not null => MatchConfidenceCategory.Possible,
            _ => null
        };

        return new AiSearchAnalytics
        {
            EventId = eventId,
            SessionId = sessionId,
            MatchesFound = matchesFound,
            SearchDurationMs = searchDurationMs,
            WasSuccessful = matchesFound > 0,
            TopMatchCategory = topCategory,
            TopSimilarityScore = topSimilarityScore,
            EmbeddingVersion = embeddingVersion,
            SearchedAt = DateTimeOffset.UtcNow
        };
    }
}
