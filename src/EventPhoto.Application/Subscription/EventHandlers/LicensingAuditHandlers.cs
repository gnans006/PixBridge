using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Events.Licensing;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Subscription.EventHandlers;

/// <summary>Writes an audit log entry when a trial subscription is started.</summary>
public sealed class TrialStartedAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<TrialStartedAuditHandler> logger)
    : INotificationHandler<TrialStartedEvent>
{
    public async Task Handle(TrialStartedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = AuditLog.Create(
                AuditAction.TrialStarted,
                "Subscription",
                $"Trial subscription started. Duration: {notification.DurationDays} days. Expires: {notification.ExpiresAt:yyyy-MM-dd}.",
                entityId: notification.SubscriptionId.ToString());

            await auditLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveAuditAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write TrialStarted audit log.");
        }
    }
}

/// <summary>Writes an audit log entry when a commercial license is activated.</summary>
public sealed class LicenseActivatedAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<LicenseActivatedAuditHandler> logger)
    : INotificationHandler<LicenseActivatedEvent>
{
    public async Task Handle(LicenseActivatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = AuditLog.Create(
                AuditAction.LicenseActivated,
                "Subscription",
                $"License activated. Plan: {notification.Plan}. Duration: {notification.DurationDays} days. " +
                $"Studio: {notification.StudioEmail}. Expires: {notification.ExpiresAt:yyyy-MM-dd}.",
                entityId: notification.SubscriptionId.ToString());

            await auditLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveAuditAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write LicenseActivated audit log.");
        }
    }
}

/// <summary>Writes an audit log entry when the trial is extended.</summary>
public sealed class TrialExtendedAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<TrialExtendedAuditHandler> logger)
    : INotificationHandler<TrialExtendedEvent>
{
    public async Task Handle(TrialExtendedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = AuditLog.Create(
                AuditAction.TrialExtended,
                "Subscription",
                $"Trial extended. New expiry: {notification.NewExpiresAt:yyyy-MM-dd}. Total duration: {notification.TotalDurationDays} days.",
                entityId: notification.SubscriptionId.ToString());

            await auditLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveAuditAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write TrialExtended audit log.");
        }
    }
}

/// <summary>Writes an audit log entry when the subscription expires.</summary>
public sealed class SubscriptionExpiredAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<SubscriptionExpiredAuditHandler> logger)
    : INotificationHandler<SubscriptionExpiredEvent>
{
    public async Task Handle(SubscriptionExpiredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = AuditLog.Create(
                AuditAction.SubscriptionExpired,
                "Subscription",
                $"Subscription expired. Plan: {notification.Plan}.",
                entityId: notification.SubscriptionId.ToString());

            await auditLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveAuditAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write SubscriptionExpired audit log.");
        }
    }
}

/// <summary>Writes an audit log entry when the subscription enters the grace period.</summary>
public sealed class SubscriptionEnteredGracePeriodAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<SubscriptionEnteredGracePeriodAuditHandler> logger)
    : INotificationHandler<SubscriptionEnteredGracePeriodEvent>
{
    public async Task Handle(SubscriptionEnteredGracePeriodEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = AuditLog.Create(
                AuditAction.SubscriptionGracePeriod,
                "Subscription",
                $"Subscription entered grace period. Days remaining in grace: {notification.DaysRemaining}.",
                entityId: notification.SubscriptionId.ToString());

            await auditLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveAuditAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write SubscriptionGracePeriod audit log.");
        }
    }
}

/// <summary>Writes an audit log entry when a clock rollback is detected.</summary>
public sealed class ClockRollbackDetectedAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<ClockRollbackDetectedAuditHandler> logger)
    : INotificationHandler<ClockRollbackDetectedEvent>
{
    public async Task Handle(ClockRollbackDetectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = AuditLog.Create(
                AuditAction.ClockRollbackDetected,
                "Subscription",
                $"Clock rollback detected. Current UTC: {notification.CurrentClockUtc:O}. " +
                $"Last validated: {notification.LastValidatedAtUtc:O}. " +
                $"Delta: {notification.DeltaHours:N1} hours.",
                entityId: notification.SubscriptionId.ToString());

            await auditLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveAuditAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write ClockRollbackDetected audit log.");
        }
    }
}

/// <summary>Writes an audit log entry when the machine fingerprint changes.</summary>
public sealed class MachineFingerprintChangedAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<MachineFingerprintChangedAuditHandler> logger)
    : INotificationHandler<MachineFingerprintChangedEvent>
{
    public async Task Handle(MachineFingerprintChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = AuditLog.Create(
                AuditAction.MachineFingerprintChanged,
                "Subscription",
                "Machine fingerprint changed — possible database migration to a different machine. " +
                "This is a warning only; the subscription continues to operate.",
                entityId: notification.InstallationId.ToString());

            await auditLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveAuditAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write MachineFingerprintChanged audit log.");
        }
    }
}

/// <summary>Writes an audit log entry when the license integrity hash does not match.</summary>
public sealed class LicenseIntegrityMismatchAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<LicenseIntegrityMismatchAuditHandler> logger)
    : INotificationHandler<LicenseIntegrityMismatchEvent>
{
    public async Task Handle(LicenseIntegrityMismatchEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = AuditLog.Create(
                AuditAction.LicenseIntegrityMismatch,
                "Subscription",
                "License integrity hash mismatch detected — possible direct database modification. " +
                "This is a warning only; the subscription continues to operate. " +
                "Please contact support for diagnostics.",
                entityId: notification.SubscriptionId.ToString());

            await auditLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveAuditAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write LicenseIntegrityMismatch audit log.");
        }
    }
}
