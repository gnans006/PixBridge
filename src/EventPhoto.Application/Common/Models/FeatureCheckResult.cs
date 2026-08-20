namespace EventPhoto.Application.Common.Models;

/// <summary>
/// Represents the result of a feature capability check performed by <see cref="Interfaces.IFeatureManager"/>.
/// </summary>
public sealed record FeatureCheckResult
{
    private FeatureCheckResult() { }

    /// <summary>Whether the feature is available to the caller.</summary>
    public bool IsAllowed { get; private init; }

    /// <summary>Human-readable reason returned to the client when <see cref="IsAllowed"/> is false.</summary>
    public string? Reason { get; private init; }

    /// <summary>Internal code useful for telemetry or mapping to HTTP status codes.</summary>
    public FeatureDenialCode DenialCode { get; private init; }

    /// <summary>Creates an allowed result.</summary>
    public static FeatureCheckResult Allow() =>
        new() { IsAllowed = true };

    /// <summary>Creates a denied result with an explicit code and user-facing reason.</summary>
    public static FeatureCheckResult Deny(FeatureDenialCode code, string reason) =>
        new() { IsAllowed = false, DenialCode = code, Reason = reason };
}

/// <summary>Reason codes returned when a feature check is denied.</summary>
public enum FeatureDenialCode
{
    None = 0,

    /// <summary>Subscription has expired — no new resources can be created.</summary>
    SubscriptionExpired = 1,

    /// <summary>Subscription was cancelled.</summary>
    SubscriptionCancelled = 2,

    /// <summary>The plan limit for this resource type has been reached.</summary>
    LimitReached = 3,

    /// <summary>The feature is not included in the current plan.</summary>
    FeatureNotInPlan = 4,
}
