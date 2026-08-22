using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Events.Licensing;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EventPhoto.Infrastructure.Persistence;

/// <summary>Seeds the database with default admin user and system settings on first run.</summary>
public static class AppDbContextSeeder
{
    /// <summary>Applies pending migrations and seeds default data.</summary>
    public static async Task SeedAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        IFingerprintService? fingerprintService = null,
        IInstallationRegistryRepository? installationRegistryRepository = null,
        ISubscriptionRepository? subscriptionRepository = null,
        IPublisher? publisher = null,
        CancellationToken cancellationToken = default)
    {
        await ApplyMigrationsAsync(context, logger, cancellationToken);
        await SeedAdminUserAsync(context, passwordHasher, logger, cancellationToken);
        await SeedSystemSettingsAsync(context, logger, cancellationToken);
        await SeedSubscriptionAsync(context, logger, cancellationToken);

        // Licensing startup checks — only run when all services are available
        if (fingerprintService is not null &&
            installationRegistryRepository is not null &&
            subscriptionRepository is not null)
        {
            await RunLicensingStartupChecksAsync(
                context, fingerprintService, installationRegistryRepository,
                subscriptionRepository, publisher, logger, cancellationToken);
        }
    }

    /// <summary>
    /// Applies pending EF migrations one at a time. If a migration fails because
    /// the pgvector extension is not installed, it is skipped and migration continues
    /// so that non-vector migrations (e.g. watermark, future tables) are still applied.
    /// </summary>
    private static async Task ApplyMigrationsAsync(AppDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0) return;

        foreach (var migration in pending)
        {
            try
            {
                await context.Database.MigrateAsync(cancellationToken);
                // MigrateAsync applies ALL pending at once; if it succeeds we're done.
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "0A000"
                                               && ex.MessageText.Contains("vector"))
            {
                logger.LogWarning(
                    "pgvector is not installed on PostgreSQL — skipping face-search migration '{Migration}'. " +
                    "Install pgvector and restart to enable face search. Error: {Message}",
                    migration, ex.MessageText);

                // Record the failed pgvector migration as applied so EF skips it next time
                // and continues to subsequent migrations.
                await context.Database.ExecuteSqlRawAsync(
                    $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                    $"VALUES ('{migration}', '8.0.11') ON CONFLICT DO NOTHING",
                    cancellationToken);

                // Retry — EF will now pick up the remaining pending migrations.
            }
        }
    }

    private static async Task SeedAdminUserAsync(AppDbContext context, IPasswordHasher passwordHasher, ILogger logger, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var admin = User.Create("admin", "admin@pixbridge.local", passwordHasher.Hash("Admin@1234!"), UserRole.Admin);
        await context.Users.AddAsync(admin, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Default admin user seeded. Username: admin / Password: Admin@1234!");
    }

    private static async Task SeedSystemSettingsAsync(AppDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        if (await context.SystemSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        var defaults = new[]
        {
            SystemSetting.Create("app.name", "PixBridge", "Application display name"),
            SystemSetting.Create("app.serverUrl", "http://192.168.10.10:5000", "LAN URL guests use to connect"),
            SystemSetting.Create("gallery.pageSize", "50", "Photos per page in gallery"),
            SystemSetting.Create("thumbnail.width", "400", "Thumbnail max width in pixels"),
            SystemSetting.Create("thumbnail.height", "400", "Thumbnail max height in pixels"),
            SystemSetting.Create("thumbnail.quality", "85", "Thumbnail JPEG quality (1-100)"),
            SystemSetting.Create("download.rateLimit", "30", "Max downloads per IP per minute"),
            SystemSetting.Create("watcher.extensions", ".jpg,.jpeg,.png,.cr2,.nef,.arw,.dng,.tiff", "Watched file extensions")
        };

        await context.SystemSettings.AddRangeAsync(defaults, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Default system settings seeded.");
    }

    private static async Task SeedSubscriptionAsync(AppDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        if (await context.Subscriptions.AnyAsync(cancellationToken))
            return;

        var sub = Domain.Entities.Subscription.CreateTrial();
        await context.Subscriptions.AddAsync(sub, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Default trial subscription seeded. Expires: {ExpiresAt:yyyy-MM-dd}", sub.ExpiresAt);
    }

    /// <summary>
    /// Runs licensing startup checks on every boot:
    /// 1. Ensure InstallationRegistry exists (first boot creates it).
    /// 2. Detect clock rollback (log warning only — never locks customer).
    /// 3. Detect machine fingerprint changes (log warning only — DB migration awareness).
    /// 4. Transition subscription state (trial → grace → expired) based on current UTC.
    /// 5. Update LastValidatedAtUtc checkpoint.
    /// </summary>
    private static async Task RunLicensingStartupChecksAsync(
        AppDbContext context,
        IFingerprintService fingerprintService,
        IInstallationRegistryRepository installationRegistryRepo,
        ISubscriptionRepository subscriptionRepo,
        IPublisher? publisher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentFingerprint = fingerprintService.ComputeHash();

            // ── 1. Installation Registry ─────────────────────────────────────
            var registry = await installationRegistryRepo.GetAsync(cancellationToken);
            if (registry is null)
            {
                registry = InstallationRegistry.Create(currentFingerprint);
                await installationRegistryRepo.AddAsync(registry, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Installation registry created. InstallationId: {Id}", registry.InstallationId);
            }
            else
            {
                // ── 3. Machine fingerprint check ────────────────────────────
                if (!string.Equals(registry.MachineFingerprintHash, currentFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Machine fingerprint changed — possible DB migration to a different machine. " +
                        "This is a warning only; subscription continues.");

                    if (publisher is not null)
                    {
                        await publisher.Publish(
                            new MachineFingerprintChangedEvent(
                                registry.InstallationId,
                                registry.MachineFingerprintHash,
                                currentFingerprint),
                            cancellationToken);
                    }

                    registry.UpdateFingerprint(currentFingerprint);
                }
                else
                {
                    registry.RecordValidation();
                }

                await installationRegistryRepo.UpdateAsync(registry, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            // ── 2. Clock rollback check ──────────────────────────────────────
            var subscription = await subscriptionRepo.GetAsync(cancellationToken);
            if (subscription is null)
                return;

            var nowUtc = DateTimeOffset.UtcNow;
            if (subscription.LastValidatedAtUtc.HasValue && nowUtc < subscription.LastValidatedAtUtc.Value)
            {
                var delta = subscription.LastValidatedAtUtc.Value - nowUtc;
                logger.LogWarning(
                    "Clock rollback detected: current UTC {Now} is {DeltaHours:N1} hours behind last validated {Last}. " +
                    "Subscription continues; anomaly has been logged.",
                    nowUtc, delta.TotalHours, subscription.LastValidatedAtUtc.Value);

                if (publisher is not null)
                {
                    await publisher.Publish(
                        new ClockRollbackDetectedEvent(
                            subscription.Id,
                            subscription.LastValidatedAtUtc.Value,
                            nowUtc,
                            delta.TotalHours),
                        cancellationToken);
                }
                // Continue — we do NOT block on clock rollback
            }

            // ── 4. Expiry state transition ───────────────────────────────────
            var stateChanged = subscription.CheckAndTransitionExpiry();

            // ── 5. Update LastValidatedAtUtc ─────────────────────────────────
            subscription.UpdateLastValidated();

            await subscriptionRepo.UpdateAsync(subscription, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            if (stateChanged)
                logger.LogInformation("Subscription state transitioned at startup. New state: {State}", subscription.State);
        }
        catch (Exception ex)
        {
            // Startup checks must NEVER prevent the application from starting
            logger.LogError(ex, "Licensing startup checks failed — application continues normally.");
        }
    }
}
