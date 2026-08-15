namespace EventPhoto.Domain.Enums;

/// <summary>Lifecycle state of the studio subscription.</summary>
public enum SubscriptionState
{
    /// <summary>Studio is on a free trial. No license key yet.</summary>
    Trial = 0,

    /// <summary>License is active and within its validity window.</summary>
    Active = 1,

    /// <summary>License has expired but is within the 7-day grace period. Full access preserved.</summary>
    GracePeriod = 2,

    /// <summary>Grace period has ended. Studio has read-only access.</summary>
    Expired = 3,

    /// <summary>License was explicitly cancelled.</summary>
    Cancelled = 4,
}
