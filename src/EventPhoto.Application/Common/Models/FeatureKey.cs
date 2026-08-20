namespace EventPhoto.Application.Common.Models;

/// <summary>
/// Keys that identify individual enforceable features/resource types.
/// Used by <see cref="Interfaces.IRequiresFeature"/> to declare which check
/// a command needs, and by <see cref="Interfaces.IFeatureManager"/> to route the check.
/// </summary>
public static class FeatureKey
{
    /// <summary>Ability to create a new photography event.</summary>
    public const string Events = "Events";

    /// <summary>Ability to create a new studio user account.</summary>
    public const string Users = "Users";

    /// <summary>Ability to start a new face search session (guest access).</summary>
    public const string FaceSearchSessions = "FaceSearchSessions";

    /// <summary>Ability to create a guest upload session.</summary>
    public const string GuestUploadSessions = "GuestUploadSessions";

    /// <summary>Access to the AI Studio premium module.</summary>
    public const string AiStudio = "AiStudio";

    /// <summary>Access to the Guest Memories premium module.</summary>
    public const string GuestMemories = "GuestMemories";
}
