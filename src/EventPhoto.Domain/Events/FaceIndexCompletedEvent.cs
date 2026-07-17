using EventPhoto.Domain.Common;

namespace EventPhoto.Domain.Events;

/// <summary>
/// Raised when all faces in a photo have been detected, embedded, and stored successfully.
/// </summary>
public sealed record FaceIndexCompletedEvent(
    Guid PhotoId,
    Guid EventId,
    int FaceCount) : IDomainEvent;
