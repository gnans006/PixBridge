namespace EventPhoto.Domain.Enums;

/// <summary>Content-moderation state of a guest-uploaded photo.</summary>
public enum ModerationStatus
{
    /// <summary>Upload has been received and is awaiting studio review.</summary>
    Pending = 0,

    /// <summary>Upload has been approved and is visible in the gallery.</summary>
    Approved = 1,

    /// <summary>Upload has been rejected and will not appear in the gallery.</summary>
    Rejected = 2,
}
