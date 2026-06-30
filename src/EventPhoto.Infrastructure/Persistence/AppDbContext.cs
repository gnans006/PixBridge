using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence;

/// <summary>Entity Framework Core DbContext for PixBridge.</summary>
public sealed class AppDbContext : DbContext, IUnitOfWork
{
    /// <summary>Initializes a new instance of <see cref="AppDbContext"/>.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Dispatches domain events and saves changes atomically.</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count != 0)
            .Select(entry => entry.Entity)
            .ToList();

        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
}
