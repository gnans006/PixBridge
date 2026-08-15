namespace EventPhoto.Contracts.Responses.GuestUpload;

/// <summary>Response shape for a single guest-submitted photo.</summary>
public sealed record GuestUploadItemResponse(
    Guid Id,
    Guid EventId,
    Guid SessionId,
    string OriginalFileName,
    string StoredPath,
    string? ThumbnailPath,
    long FileSizeBytes,
    string ContentType,
    DateTimeOffset UploadedAt,
    string ModerationStatus,
    string? RejectionReason);
