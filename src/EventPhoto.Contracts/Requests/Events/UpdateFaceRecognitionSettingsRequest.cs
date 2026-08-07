namespace EventPhoto.Contracts.Requests.Events;

/// <summary>Request to update the face recognition settings for an event.</summary>
public sealed record UpdateFaceRecognitionSettingsRequest(
    bool EnableFaceRecognition,
    float FaceMatchThreshold,
    bool AllowFaceSearch);
