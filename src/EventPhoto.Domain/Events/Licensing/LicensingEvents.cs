using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;

namespace EventPhoto.Domain.Events.Licensing;

public sealed record TrialStartedEvent(
    Guid SubscriptionId,
    DateTimeOffset ExpiresAt,
    int DurationDays) : IDomainEvent;

public sealed record LicenseActivatedEvent(
    Guid SubscriptionId,
    SubscriptionPlan Plan,
    int DurationDays,
    DateTimeOffset ActivatedAt,
    DateTimeOffset ExpiresAt,
    string? StudioEmail) : IDomainEvent;

public sealed record TrialExtendedEvent(
    Guid SubscriptionId,
    DateTimeOffset NewExpiresAt,
    int TotalDurationDays) : IDomainEvent;

public sealed record SubscriptionExpiredEvent(
    Guid SubscriptionId,
    SubscriptionPlan Plan) : IDomainEvent;

public sealed record SubscriptionEnteredGracePeriodEvent(
    Guid SubscriptionId,
    SubscriptionPlan Plan,
    int DaysRemaining) : IDomainEvent;

public sealed record ClockRollbackDetectedEvent(
    Guid SubscriptionId,
    DateTimeOffset LastValidatedAtUtc,
    DateTimeOffset CurrentClockUtc,
    double DeltaHours) : IDomainEvent;

public sealed record MachineFingerprintChangedEvent(
    Guid InstallationId,
    string OldFingerprintHash,
    string NewFingerprintHash) : IDomainEvent;

public sealed record LicenseIntegrityMismatchEvent(
    Guid SubscriptionId,
    string Plan) : IDomainEvent;
