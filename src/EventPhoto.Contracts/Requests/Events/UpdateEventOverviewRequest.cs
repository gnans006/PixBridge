namespace EventPhoto.Contracts.Requests.Events;

/// <summary>Request to update an event's core information fields.</summary>
public sealed record UpdateEventOverviewRequest(
    string Name,
    string EventType,
    DateOnly EventDate,
    string? Description,
    string? VenueName,
    string? ClientName);
