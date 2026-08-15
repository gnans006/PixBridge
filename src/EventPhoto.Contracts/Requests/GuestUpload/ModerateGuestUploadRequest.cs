namespace EventPhoto.Contracts.Requests.GuestUpload;

/// <summary>Payload for moderating a guest upload (approve or reject).</summary>
public sealed record ModerateGuestUploadRequest(
    bool Approve,
    string? RejectionReason = null);
