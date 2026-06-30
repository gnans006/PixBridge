using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the PixBridge application, backed by PostgreSQL.
/// Also implements <see cref="IUnitOfWork"/> to coordinate saves across repositories.
/// </summary>
public sealed class PixBridgeDbContext : DbContext, IUnitOfWork
{
    /// <summary>
    /// Initializes a new instance of <see cref="PixBridgeDbContext"/>.
    /// </summary>
    /// <param name="options">The context options.</param>
    public PixBridgeDbContext(DbContextOptions<PixBridgeDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the events set.</summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>Gets the photos set.</summary>
    public DbSet<Photo> Photos => Set<Photo>();

    /// <summary>Gets the users set.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the download logs set.</summary>
    public DbSet<DownloadLog> DownloadLogs => Set<DownloadLog>();

    /// <summary>Gets the system settings set.</summary>
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PixBridgeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
        => base.SaveChangesAsync(cancellationToken);
}
