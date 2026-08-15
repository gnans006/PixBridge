namespace EventPhoto.Domain.Enums;

/// <summary>Commercial subscription plan tier.</summary>
public enum SubscriptionPlan
{
    /// <summary>Free trial — limited to 5 events and 2 users.</summary>
    Trial = 0,

    /// <summary>Starter plan — 20 events, 5 users.</summary>
    Starter = 1,

    /// <summary>Professional plan — 100 events, unlimited users.</summary>
    Professional = 2,

    /// <summary>Enterprise plan — unlimited everything.</summary>
    Enterprise = 3,
}
