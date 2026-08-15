namespace EventPhoto.Domain.Enums;

/// <summary>Lifecycle state of a guest photo-upload session.</summary>
public enum GuestUploadSessionStatus
{
    /// <summary>Session is open; guests may still submit photos.</summary>
    Active = 0,

    /// <summary>Session has been closed; no further uploads accepted.</summary>
    Closed = 1,
}
