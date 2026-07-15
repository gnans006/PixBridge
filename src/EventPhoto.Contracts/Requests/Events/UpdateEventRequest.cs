namespace EventPhoto.Contracts.Requests.Events;

/// <summary>Request model for updating an existing event.</summary>
public sealed record UpdateEventRequest(
    string Name,
    string EventType,
    DateOnly EventDate,
    string? Description,
    string? VenueName,
    string? ClientName,
    int? GalleryRecentCount);
