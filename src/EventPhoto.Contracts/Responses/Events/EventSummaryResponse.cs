namespace EventPhoto.Contracts.Responses.Events;

/// <summary>
/// Lightweight event summary for list views.
/// </summary>
/// <param name="Id">The event identifier.</param>
/// <param name="Name">The event display name.</param>
/// <param name="EventType">The event type name.</param>
/// <param name="EventDate">The event date.</param>
/// <param name="ClientName">The optional client name.</param>
/// <param name="IsActive">Whether the event is currently active.</param>
/// <param name="PhotoCount">Cached total photo count.</param>
public sealed record EventSummaryResponse(
    Guid Id,
    string Name,
    string EventType,
    DateOnly EventDate,
    string? ClientName,
    bool IsActive,
    int PhotoCount);
