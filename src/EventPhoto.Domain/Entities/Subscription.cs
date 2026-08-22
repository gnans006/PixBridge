using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Events.Licensing;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Singleton subscription record that tracks the studio's commercial license.
/// Only one record ever exists. <see cref="SingletonId"/> is always used as the primary key.
///
/// <para>Expiry is driven by <see cref="ExpiresAt"/> (set by the server, never by the client).
/// State transitions happen via <see cref="CheckAndTransitionExpiry"/> called at startup —
/// no background service or heartbeat dependency.</para>
/// </summary>
public sealed class Subscription : AggregateRoot
{
    /// <summary>Well-known fixed ID for the singleton subscription record.</summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000002");

    private const int GracePeriodDays = 7;

    private Subscription() { }

    // ── Plan & State ──────────────────────────────────────────────────────────

    /// <summary>Gets the commercial plan tier.</summary>
    public SubscriptionPlan Plan { get; private set; }

    /// <summary>Gets the current lifecycle state.</summary>
    public SubscriptionState State { get; private set; }

    // ── License Identity ──────────────────────────────────────────────────────

    /// <summary>Gets the license key used for activation, if any.</summary>
    public string? LicenseKey { get; private set; }

    /// <summary>Gets the studio email associated with the license.</summary>
    public string? StudioEmail { get; private set; }

    /// <summary>
    /// Installation ID recorded at activation time.
    /// Used to detect database copies to different machines.
    /// </summary>
    public Guid? InstallationId { get; private set; }

    /// <summary>
    /// SHA-256 hardware fingerprint hash recorded at activation.
    /// Compared on startup to detect machine changes.
    /// </summary>
    public string? MachineFingerprintHash { get; private set; }

    // ── Duration Accounting ───────────────────────────────────────────────────

    /// <summary>Gets the UTC timestamp when the license was activated (set by the server, never the client).</summary>
    public DateTimeOffset? ActivatedAt { get; private set; }

    /// <summary>Licensed duration in calendar days. Derived from the license key payload.</summary>
    public int DurationDays { get; private set; }

    /// <summary>Gets the UTC timestamp when the license expires (ActivatedAt + DurationDays).</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>Gets the UTC timestamp when the grace period ends (ExpiresAt + 7 days).</summary>
    public DateTimeOffset? GracePeriodEndsAt { get; private set; }

    // ── Limits ────────────────────────────────────────────────────────────────

    /// <summary>Gets the maximum number of active events (0 = unlimited).</summary>
    public int MaxEvents { get; private set; }

    /// <summary>Gets the maximum number of studio users (0 = unlimited).</summary>
    public int MaxUsersPerStudio { get; private set; }

    // ── Integrity & Clock ─────────────────────────────────────────────────────

    /// <summary>
    /// HMAC of key license fields, used to detect direct DB tampering.
    /// Verified at startup — mismatch generates an audit warning only, never locks the customer.
    /// </summary>
    public string? LicenseIntegrityHash { get; private set; }

    /// <summary>
    /// UTC timestamp updated on every successful startup validation.
    /// Used for clock-rollback detection: if current UTC &lt; LastValidatedAtUtc,
    /// the system clock was rolled back.
    /// </summary>
    public DateTimeOffset? LastValidatedAtUtc { get; private set; }

    // ── Trial Extension ───────────────────────────────────────────────────────

    /// <summary>
    /// Whether the one-time 15-day trial extension has been consumed.
    /// Prevents a second extension.
    /// </summary>
    public bool HasUsedTrialExtension { get; private set; }

    /// <summary>Gets optional internal notes about the subscription.</summary>
    public string? Notes { get; private set; }

    // ── Computed ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns whether new commercial operations are currently allowed.
    /// True for Trial, Active, and GracePeriod states.
    /// </summary>
    public bool IsOperational =>
        State is SubscriptionState.Trial
               or SubscriptionState.Active
               or SubscriptionState.GracePeriod;

    /// <summary>
    /// Days remaining until expiry. Null if no expiry set (should not occur after CreateTrial fix).
    /// </summary>
    public int? DaysRemaining =>
        ExpiresAt.HasValue
            ? Math.Max(0, (int)(ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays)
            : null;

    /// <summary>Days remaining in grace period (0 if not in grace period).</summary>
    public int GracePeriodDaysRemaining =>
        State == SubscriptionState.GracePeriod && GracePeriodEndsAt.HasValue
            ? Math.Max(0, (int)(GracePeriodEndsAt.Value - DateTimeOffset.UtcNow).TotalDays)
            : 0;

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the default 30-day trial subscription seeded on first run.
    /// Sets <see cref="ActivatedAt"/>, <see cref="ExpiresAt"/>, and <see cref="GracePeriodEndsAt"/>
    /// so the trial expires automatically at startup without a background service.
    /// </summary>
    public static Subscription CreateTrial()
    {
        var now     = DateTimeOffset.UtcNow;
        var expires = now.AddDays(30);
        var sub = new Subscription
        {
            Id                    = SingletonId,
            Plan                  = SubscriptionPlan.Trial,
            State                 = SubscriptionState.Trial,
            DurationDays          = 30,
            ActivatedAt           = now,
            ExpiresAt             = expires,
            GracePeriodEndsAt     = expires.AddDays(GracePeriodDays),
            MaxEvents             = 5,
            MaxUsersPerStudio     = 3,
            HasUsedTrialExtension = false,
            LastValidatedAtUtc    = now,
            CreatedAt             = now,
            UpdatedAt             = now,
        };
        sub.RaiseDomainEvent(new TrialStartedEvent(sub.Id, expires, 30));
        return sub;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Activates the subscription from a decoded license payload.
    /// <para><paramref name="durationDays"/> and <paramref name="plan"/> come from the
    /// HMAC-verified license key — never from the client request directly.</para>
    /// </summary>
    public void Activate(
        string licenseKey,
        string studioEmail,
        SubscriptionPlan plan,
        int durationDays,
        Guid? installationId = null,
        string? machineFingerprintHash = null,
        string? integrityHash = null)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            throw new DomainException("License key is required.");
        if (string.IsNullOrWhiteSpace(studioEmail))
            throw new DomainException("Studio email is required.");
        if (durationDays < 1)
            throw new DomainException("License duration must be at least 1 day.");

        var now     = DateTimeOffset.UtcNow;
        var expires = now.AddDays(durationDays);

        LicenseKey              = licenseKey.Trim();
        StudioEmail             = studioEmail.Trim().ToLowerInvariant();
        Plan                    = plan;
        State                   = SubscriptionState.Active;
        DurationDays            = durationDays;
        ActivatedAt             = now;
        ExpiresAt               = expires;
        GracePeriodEndsAt       = expires.AddDays(GracePeriodDays);
        InstallationId          = installationId;
        MachineFingerprintHash  = machineFingerprintHash;
        LicenseIntegrityHash    = integrityHash;
        LastValidatedAtUtc      = now;
        (MaxEvents, MaxUsersPerStudio) = PlanLimits(plan);
        Touch();

        RaiseDomainEvent(new LicenseActivatedEvent(Id, plan, durationDays, now, expires, studioEmail));
    }

    /// <summary>
    /// Grants the one-time 15-day trial extension.
    /// Extends both <see cref="ExpiresAt"/> and <see cref="GracePeriodEndsAt"/>.
    /// </summary>
    public void ExtendTrial()
    {
        if (HasUsedTrialExtension)
            throw new DomainException("The trial extension has already been used. Only one extension is allowed.");

        if (State is not (SubscriptionState.Trial or SubscriptionState.GracePeriod))
            throw new DomainException("Trial extension is only available during Trial or Grace Period.");

        const int extensionDays = 15;

        // Extend from current ExpiresAt (or now if already past)
        var baseDate = ExpiresAt.HasValue && ExpiresAt.Value > DateTimeOffset.UtcNow
            ? ExpiresAt.Value
            : DateTimeOffset.UtcNow;

        var newExpires    = baseDate.AddDays(extensionDays);
        DurationDays     += extensionDays;
        ExpiresAt         = newExpires;
        GracePeriodEndsAt = newExpires.AddDays(GracePeriodDays);
        Plan              = SubscriptionPlan.ExtendedTrial;
        HasUsedTrialExtension = true;

        // If we were in grace period, return to Trial state (extension refreshed it)
        if (State == SubscriptionState.GracePeriod)
            State = SubscriptionState.Trial;

        Touch();
        RaiseDomainEvent(new TrialExtendedEvent(Id, newExpires, DurationDays));
    }

    /// <summary>
    /// Transitions to <see cref="SubscriptionState.GracePeriod"/>.
    /// Called by <see cref="CheckAndTransitionExpiry"/>.
    /// </summary>
    public void EnterGracePeriod()
    {
        if (State is not (SubscriptionState.Trial or SubscriptionState.Active))
            return;

        State = SubscriptionState.GracePeriod;
        Touch();
        RaiseDomainEvent(new SubscriptionEnteredGracePeriodEvent(Id, Plan, GracePeriodDaysRemaining));
    }

    /// <summary>
    /// Transitions to <see cref="SubscriptionState.Expired"/>.
    /// Called by <see cref="CheckAndTransitionExpiry"/>.
    /// </summary>
    public void Expire()
    {
        if (State == SubscriptionState.Expired)
            return;

        State = SubscriptionState.Expired;
        Touch();
        RaiseDomainEvent(new SubscriptionExpiredEvent(Id, Plan));
    }

    /// <summary>
    /// Performs startup-time expiry state transitions based on the current UTC clock.
    /// Returns <see langword="true"/> if the state changed and the record must be persisted.
    ///
    /// <para>This method is the ONLY place state transitions happen — no background service needed.
    /// Call once per application startup in the seeder/startup pipeline.</para>
    /// </summary>
    public bool CheckAndTransitionExpiry()
    {
        if (!ExpiresAt.HasValue)
            return false; // legacy row without expiry — leave untouched

        var now = DateTimeOffset.UtcNow;

        // Already in terminal states — nothing to do
        if (State is SubscriptionState.Expired or SubscriptionState.Cancelled)
            return false;

        // Within grace period window: ExpiresAt < now <= GracePeriodEndsAt
        if (now > ExpiresAt.Value && GracePeriodEndsAt.HasValue && now <= GracePeriodEndsAt.Value)
        {
            if (State != SubscriptionState.GracePeriod)
            {
                EnterGracePeriod();
                return true;
            }
            return false;
        }

        // Grace period itself has ended
        if (GracePeriodEndsAt.HasValue && now > GracePeriodEndsAt.Value)
        {
            if (State != SubscriptionState.Expired)
            {
                Expire();
                return true;
            }
            return false;
        }

        return false;
    }

    /// <summary>
    /// Updates <see cref="LastValidatedAtUtc"/> to the current UTC time.
    /// Call on every startup after clock rollback detection passes.
    /// </summary>
    public void UpdateLastValidated()
    {
        LastValidatedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public static (int maxEvents, int maxUsers) PlanLimits(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Trial         => (5, 3),
        SubscriptionPlan.ExtendedTrial => (5, 3),
        SubscriptionPlan.Professional  => (100, 10),
        SubscriptionPlan.Premium       => (0, 0),
        _                              => (5, 3),
    };
}