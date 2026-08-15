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

    // ── Face Recognition (legacy) ─────────────────────────────────────────────

    /// <summary>Gets the face embedding vectors set.</summary>
    public DbSet<FaceEmbedding> FaceEmbeddings => Set<FaceEmbedding>();

    /// <summary>Gets the guest face-search sessions set.</summary>
    public DbSet<GuestFaceSession> GuestFaceSessions => Set<GuestFaceSession>();

    /// <summary>Gets the photo matches set.</summary>
    public DbSet<PhotoMatch> PhotoMatches => Set<PhotoMatch>();

    // ── AI Discovery Engine ───────────────────────────────────────────────────

    /// <summary>Gets the AI Discovery Pipeline processing jobs set.</summary>
    public DbSet<FaceProcessingJob> FaceProcessingJobs => Set<FaceProcessingJob>();

    /// <summary>Gets the face clusters set (future-ready clustering).</summary>
    public DbSet<FaceCluster> FaceClusters => Set<FaceCluster>();

    /// <summary>Gets the AI search analytics set.</summary>
    public DbSet<AiSearchAnalytics> AiSearchAnalytics => Set<AiSearchAnalytics>();

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Gets the watermark configurations set.</summary>
    public DbSet<WatermarkConfiguration> WatermarkConfigurations => Set<WatermarkConfiguration>();

    /// <summary>Gets the application settings singleton record.</summary>
    public DbSet<ApplicationSettings> ApplicationSettings => Set<ApplicationSettings>();

    /// <summary>Gets the audit log entries set.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ── Phase 4 — Guest Experience ────────────────────────────────────────────

    /// <summary>Gets the guest upload sessions set.</summary>
    public DbSet<GuestUploadSession> GuestUploadSessions => Set<GuestUploadSession>();

    /// <summary>Gets the guest uploads (photos submitted by guests) set.</summary>
    public DbSet<GuestUpload> GuestUploads => Set<GuestUpload>();

    // ── Phase 5 — Subscription Engine ────────────────────────────────────────

    /// <summary>Gets the subscription singleton record.</summary>
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.HasPostgresExtension("vector");
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
