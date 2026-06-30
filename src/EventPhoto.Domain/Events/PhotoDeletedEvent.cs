using EventPhoto.Domain.Common;

namespace EventPhoto.Domain.Events;

/// <summary>
/// Raised when a photo is soft-deleted.
/// </summary>
/// <param name="PhotoId">The unique identifier of the photo.</param>
/// <param name="EventId">The unique identifier of the parent event.</param>
public sealed record PhotoDeletedEvent(Guid PhotoId, Guid EventId) : IDomainEvent;
