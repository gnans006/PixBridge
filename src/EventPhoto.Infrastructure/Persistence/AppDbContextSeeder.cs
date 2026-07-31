using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EventPhoto.Infrastructure.Persistence;

/// <summary>Seeds the database with default admin user and system settings on first run.</summary>
public static class AppDbContextSeeder
{
    /// <summary>Applies pending migrations and seeds default data.</summary>
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher, ILogger logger, CancellationToken cancellationToken = default)
    {
        await ApplyMigrationsAsync(context, logger, cancellationToken);
        await SeedAdminUserAsync(context, passwordHasher, logger, cancellationToken);
        await SeedSystemSettingsAsync(context, logger, cancellationToken);
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
}
