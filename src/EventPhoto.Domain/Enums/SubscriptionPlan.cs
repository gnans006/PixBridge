namespace EventPhoto.Domain.Enums;

/// <summary>Commercial subscription plan tier.</summary>
public enum SubscriptionPlan
{
    /// <summary>Free 30-day trial — 5 events, 3 users, unlimited face searches.</summary>
    Trial = 0,

    /// <summary>Extended trial — 15 additional days granted one time only.</summary>
    ExtendedTrial = 1,

    /// <summary>Professional plan — 100 events, unlimited users.</summary>
    Professional = 2,

    /// <summary>Premium plan — unlimited everything, all features.</summary>
    Premium = 3,
}
