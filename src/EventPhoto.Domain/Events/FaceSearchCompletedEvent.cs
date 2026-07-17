using EventPhoto.Domain.Common;

namespace EventPhoto.Domain.Events;

/// <summary>
/// Raised when a guest face-search session completes and matched photos are available.
/// </summary>
public sealed record FaceSearchCompletedEvent(
    Guid SessionId,
    Guid EventId,
    string SessionToken,
    int MatchCount) : IDomainEvent;
