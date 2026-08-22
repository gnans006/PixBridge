namespace EventPhoto.Contracts.Requests.Subscription;

/// <summary>Payload for activating a commercial license.</summary>
public sealed record ActivateSubscriptionRequest(
    string LicenseKey,
    string StudioEmail);
