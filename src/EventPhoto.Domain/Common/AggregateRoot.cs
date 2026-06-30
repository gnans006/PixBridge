namespace EventPhoto.Domain.Common;

/// <summary>
/// Aggregate root that can raise domain events.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the read-only collection of uncommitted domain events.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all uncommitted domain events after they have been dispatched.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
