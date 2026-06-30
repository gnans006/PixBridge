using EventPhoto.Domain.Common;

namespace EventPhoto.Domain.Events;

/// <summary>
/// Raised when an event is deactivated and no longer accepts new photos.
/// </summary>
/// <param name="EventId">The unique identifier of the event.</param>
public sealed record EventDeactivatedEvent(Guid EventId) : IDomainEvent;
