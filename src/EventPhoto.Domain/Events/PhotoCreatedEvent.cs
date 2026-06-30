using EventPhoto.Domain.Common;

namespace EventPhoto.Domain.Events;

/// <summary>
/// Raised when a new photo is detected and saved to the system.
/// </summary>
/// <param name="PhotoId">The unique identifier of the photo.</param>
/// <param name="EventId">The unique identifier of the parent event.</param>
/// <param name="FileName">The detected file name.</param>
public sealed record PhotoCreatedEvent(Guid PhotoId, Guid EventId, string FileName) : IDomainEvent;
