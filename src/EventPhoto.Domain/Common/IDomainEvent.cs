using MediatR;

namespace EventPhoto.Domain.Common;

/// <summary>
/// Marker interface for domain events dispatched through MediatR.
/// </summary>
public interface IDomainEvent : INotification
{
}
