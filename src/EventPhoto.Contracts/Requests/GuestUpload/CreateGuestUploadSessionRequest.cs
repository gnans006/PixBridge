namespace EventPhoto.Contracts.Requests.GuestUpload;

/// <summary>Payload for creating a new guest upload session.</summary>
public sealed record CreateGuestUploadSessionRequest(
    string? Title);
