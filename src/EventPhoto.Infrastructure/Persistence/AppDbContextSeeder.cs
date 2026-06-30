using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Infrastructure.Persistence;

/// <summary>Seeds the database with default admin user and system settings on first run.</summary>
public static class AppDbContextSeeder
{
    /// <summary>Applies pending migrations and seeds default data.</summary>
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher, ILogger logger, CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);
        await SeedAdminUserAsync(context, passwordHasher, logger, cancellationToken);
        await SeedSystemSettingsAsync(context, logger, cancellationToken);
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
            SystemSetting.Create("app.serverUrl", "http://192.168.10.10", "LAN URL guests use to connect"),
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
