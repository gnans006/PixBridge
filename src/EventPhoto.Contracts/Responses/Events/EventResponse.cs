namespace EventPhoto.Contracts.Responses.Events;

/// <summary>Event summary returned by the API.</summary>
public sealed record EventResponse(
    Guid Id,
    string Name,
    string? Description,
    string EventType,
    DateOnly EventDate,
    string? VenueName,
    string? ClientName,
    string WatchFolder,
    string? QrCodeUrl,
    bool IsActive,
    int PhotoCount,
    string TotalSize,
    DateTimeOffset CreatedAt);
