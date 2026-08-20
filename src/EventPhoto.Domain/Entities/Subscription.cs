using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Singleton subscription record that tracks the studio's commercial license.
/// Only one record ever exists. <see cref="SingletonId"/> is always used as the primary key.
/// </summary>
public sealed class Subscription : AggregateRoot
{
    /// <summary>Well-known fixed ID for the singleton subscription record.</summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000002");

    private Subscription() { }

    /// <summary>Gets the commercial plan tier.</summary>
    public SubscriptionPlan Plan { get; private set; }

    /// <summary>Gets the current lifecycle state.</summary>
    public SubscriptionState State { get; private set; }

    /// <summary>Gets the license key used for activation, if any.</summary>
    public string? LicenseKey { get; private set; }

    /// <summary>Gets the studio email associated with the license.</summary>
    public string? StudioEmail { get; private set; }

    /// <summary>Gets the UTC timestamp when the license was first activated.</summary>
    public DateTimeOffset? ActivatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when the current license period expires.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>Gets the UTC timestamp when the grace period ends (ActivatedAt + validity + 7 days).</summary>
    public DateTimeOffset? GracePeriodEndsAt { get; private set; }

    /// <summary>Gets the maximum number of active events (0 = unlimited).</summary>
    public int MaxEvents { get; private set; }

    /// <summary>Gets the maximum number of studio users (0 = unlimited).</summary>
    public int MaxUsersPerStudio { get; private set; }

    /// <summary>Gets optional internal notes about the subscription.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Whether the one-time 15-day trial extension has already been consumed.
    /// Set to <c>true</c> by <see cref="ExtendTrial"/>; prevents a second extension.
    /// </summary>
    public bool HasUsedTrialExtension { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>Creates the default trial subscription seeded on first run.</summary>
    public static Subscription CreateTrial()
    {
        return new Subscription
        {
            Id                    = SingletonId,
            Plan                  = SubscriptionPlan.Trial,
            State                 = SubscriptionState.Trial,
            MaxEvents             = 5,
            MaxUsersPerStudio     = 3,
            HasUsedTrialExtension = false,
            CreatedAt             = DateTimeOffset.UtcNow,
            UpdatedAt             = DateTimeOffset.UtcNow,
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>Activates the subscription with the given license details.</summary>
    public void Activate(
        string licenseKey,
        string studioEmail,
        SubscriptionPlan plan,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            throw new DomainException("License key is required.");
        if (string.IsNullOrWhiteSpace(studioEmail))
            throw new DomainException("Studio email is required.");
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new DomainException("Expiry date must be in the future.");

        LicenseKey        = licenseKey.Trim();
        StudioEmail       = studioEmail.Trim().ToLowerInvariant();
        Plan              = plan;
        State             = SubscriptionState.Active;
        ActivatedAt       = DateTimeOffset.UtcNow;
        ExpiresAt         = expiresAt;
        GracePeriodEndsAt = expiresAt.AddDays(7);
        (MaxEvents, MaxUsersPerStudio) = PlanLimits(plan);
        Touch();
    }

    /// <summary>Transitions the subscription into grace period (called by the background service on expiry).</summary>
    public void EnterGracePeriod()
    {
        if (State != SubscriptionState.Active)
            return;

        State = SubscriptionState.GracePeriod;
        Touch();
    }

    /// <summary>Marks the subscription as expired (called when grace period ends).</summary>
    public void Expire()
    {
        if (State != SubscriptionState.GracePeriod)
            return;

        State = SubscriptionState.Expired;
        Touch();
    }

    /// <summary>
    /// Grants a one-time 15-day trial extension. Transitions the plan to
    /// <see cref="SubscriptionPlan.ExtendedTrial"/> and updates the expiry.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown if the extension has already been used or the subscription is not in Trial state.
    /// </exception>
    public void ExtendTrial()
    {
        if (HasUsedTrialExtension)
            throw new DomainException("The trial extension has already been used. Only one extension is allowed.");

        if (State != SubscriptionState.Trial)
            throw new DomainException("Trial extension is only available while the subscription is in Trial state.");

        Plan                  = SubscriptionPlan.ExtendedTrial;
        HasUsedTrialExtension = true;
        (MaxEvents, MaxUsersPerStudio) = PlanLimits(SubscriptionPlan.ExtendedTrial);
        Touch();
    }

    /// <summary>
    /// Returns whether an operation is currently allowed.
    /// During grace period everything is still allowed; only fully Expired / Cancelled is restricted.
    /// </summary>
    public bool IsOperational =>
        State is SubscriptionState.Trial
               or SubscriptionState.Active
               or SubscriptionState.GracePeriod;

    /// <summary>Returns the number of days remaining in the grace period (0 if not applicable).</summary>
    public int GracePeriodDaysRemaining =>
        State == SubscriptionState.GracePeriod && GracePeriodEndsAt.HasValue
            ? Math.Max(0, (int)(GracePeriodEndsAt.Value - DateTimeOffset.UtcNow).TotalDays)
            : 0;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (int maxEvents, int maxUsers) PlanLimits(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Trial         => (5, 3),
        SubscriptionPlan.ExtendedTrial => (5, 3),
        SubscriptionPlan.Professional  => (100, 0),
        SubscriptionPlan.Premium       => (0, 0),
        _                              => (5, 3),
    };
}
