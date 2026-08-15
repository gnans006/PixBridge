namespace EventPhoto.Contracts.Responses.Subscription;

/// <summary>Response shape for the studio subscription.</summary>
public sealed record SubscriptionResponse(
    string Plan,
    string State,
    string? LicenseKey,
    string? StudioEmail,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? GracePeriodEndsAt,
    int MaxEvents,
    int MaxUsersPerStudio,
    bool IsOperational,
    int GracePeriodDaysRemaining,
    string? Notes);
