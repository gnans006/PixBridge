using EventPhoto.Domain.Common;

namespace EventPhoto.Domain.Events;

/// <summary>
/// Raised when a new photography event is created.
/// </summary>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="EventName">The event display name.</param>
public sealed record EventCreatedEvent(Guid EventId, string EventName) : IDomainEvent;
