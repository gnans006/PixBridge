namespace EventPhoto.Contracts.Responses.GuestUpload;

/// <summary>Response shape for a single guest upload session.</summary>
public sealed record GuestUploadSessionResponse(
    Guid Id,
    Guid EventId,
    string SessionCode,
    string? Title,
    int PhotoCount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt);
