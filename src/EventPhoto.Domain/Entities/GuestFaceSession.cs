using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;
using Pgvector;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Represents a guest face-search session for the "Find My Photos™" experience.
/// Created when a guest uploads their selfie; tracks the search lifecycle,
/// analytics, and authorises restricted downloads when required.
///
/// <para>Privacy: selfie embedding is stored only for the session lifetime.
/// <see cref="SelfieDeletedAt"/> tracks when the embedding was purged per the retention policy.</para>
/// </summary>
public sealed class GuestFaceSession : Entity
{
    private GuestFaceSession()
    {
    }

    /// <summary>Gets the identifier of the event being searched.</summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Gets the opaque session token issued to the guest.
    /// Used to authorise download requests when <c>RestrictDownloadsToMatchedPhotos</c> is enabled.
    /// </summary>
    public string SessionToken { get; private set; } = string.Empty;

    /// <summary>Gets the current status of this session.</summary>
    public FaceSessionStatus Status { get; private set; } = FaceSessionStatus.Created;

    /// <summary>
    /// Gets the 512-dimensional embedding vector generated from the guest's selfie.
    /// Used to perform the pgvector cosine-similarity search.
    /// PRIVACY: purged after <see cref="SelfieRetentionHours"/> by the retention service.
    /// </summary>
    public Vector SelfieEmbedding { get; private set; } = new Vector(new float[512]);

    /// <summary>
    /// Gets the SHA-256 hash of the original selfie bytes (hex string, 64 chars).
    /// Used for result caching — repeated identical selfie uploads skip the embedding + search steps.
    /// </summary>
    public string? SelfieHash { get; private set; }

    /// <summary>Gets the UTC timestamp when the vector search started.</summary>
    public DateTimeOffset? SearchStartedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when the vector search completed.</summary>
    public DateTimeOffset? SearchCompletedAt { get; private set; }

    /// <summary>Gets the UTC timestamp after which this session is no longer valid.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Gets the number of matched photos found by the search.</summary>
    public int MatchCount { get; private set; }

    /// <summary>Gets the end-to-end search duration in milliseconds (0 when not yet completed).</summary>
    public int SearchDurationMs { get; private set; }

    /// <summary>
    /// Gets when the selfie embedding was zeroed out by the retention service.
    /// <c>null</c> means the embedding is still present.
    /// </summary>
    public DateTimeOffset? SelfieDeletedAt { get; private set; }

    /// <summary>Gets the navigation property to the parent event.</summary>
    public Event? Event { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new guest face-search session.
    /// </summary>
    /// <param name="eventId">The event being searched.</param>
    /// <param name="selfieEmbedding">512-dimensional embedding from the guest's selfie.</param>
    /// <param name="selfieHash">SHA-256 hex hash of the raw selfie bytes for cache lookup.</param>
    /// <param name="sessionTtlMinutes">How long the session remains valid (default 60 minutes).</param>
    public static GuestFaceSession Create(
        Guid eventId,
        float[] selfieEmbedding,
        string? selfieHash = null,
        int sessionTtlMinutes = 60)
    {
        if (eventId == Guid.Empty)
            throw new DomainException("EventId is required.");

        if (selfieEmbedding is null || selfieEmbedding.Length != 512)
            throw new DomainException("Selfie embedding must be a 512-dimensional vector.");

        if (sessionTtlMinutes < 1)
            throw new DomainException("Session TTL must be at least 1 minute.");

        return new GuestFaceSession
        {
            EventId = eventId,
            SessionToken = Guid.NewGuid().ToString("N"),
            SelfieEmbedding = new Vector(selfieEmbedding),
            SelfieHash = selfieHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(sessionTtlMinutes)
        };
    }

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>Transitions the session to <see cref="FaceSessionStatus.Searching"/>.</summary>
    public void MarkSearching()
    {
        Status = FaceSessionStatus.Searching;
        SearchStartedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Transitions the session to <see cref="FaceSessionStatus.Completed"/> and records analytics.
    /// </summary>
    /// <param name="matchCount">Number of photos matched.</param>
    public void MarkCompleted(int matchCount)
    {
        if (matchCount < 0)
            throw new DomainException("Match count cannot be negative.");

        Status = FaceSessionStatus.Completed;
        SearchCompletedAt = DateTimeOffset.UtcNow;
        MatchCount = matchCount;

        if (SearchStartedAt.HasValue)
        {
            SearchDurationMs = (int)(SearchCompletedAt.Value - SearchStartedAt.Value).TotalMilliseconds;
        }

        Touch();
    }

    /// <summary>Marks the session as expired so it can no longer authorise downloads.</summary>
    public void MarkExpired()
    {
        Status = FaceSessionStatus.Expired;
        Touch();
    }

    /// <summary>
    /// Purges the selfie embedding by zeroing it out for privacy compliance.
    /// Called by the retention service after the configured TTL.
    /// </summary>
    public void PurgeSelfieEmbedding()
    {
        SelfieEmbedding = new Vector(new float[512]);
        SelfieDeletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Returns <c>true</c> if the session is past its expiry timestamp
    /// OR has been explicitly marked <see cref="FaceSessionStatus.Expired"/>.
    /// </summary>
    public bool IsExpired =>
        Status == FaceSessionStatus.Expired || DateTimeOffset.UtcNow > ExpiresAt;
}
