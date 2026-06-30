namespace EventPhoto.Contracts.Requests.Events;

/// <summary>Request model for creating a new photography event.</summary>
public sealed record CreateEventRequest(
    string Name,
    string EventType,
    DateOnly EventDate,
    string WatchFolder,
    string? Description,
    string? VenueName,
    string? ClientName);
