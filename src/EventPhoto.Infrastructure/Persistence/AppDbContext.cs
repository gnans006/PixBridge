using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence;

/// <summary>Entity Framework Core DbContext for PixBridge.</summary>
public sealed class AppDbContext : DbContext, IUnitOfWork
{
    private readonly IPublisher? _publisher;

    /// <summary>
    /// Design-time / migration constructor. Domain events are collected but not dispatched.
    /// Used by <see cref="AppDbContextFactory"/> and EF Core CLI tools.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Runtime constructor used by DI. Domain events are dispatched through MediatR
    /// after each successful <see cref="SaveChangesAsync"/> call.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
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

    /// <summary>Gets the installation registry singleton record.</summary>
    public DbSet<InstallationRegistry> InstallationRegistries => Set<InstallationRegistry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.HasPostgresExtension("vector");
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Saves all changes and dispatches any domain events raised by aggregates.
    ///
    /// <para>Order of operations:</para>
    /// <list type="number">
    ///   <item>Collect domain events from all tracked aggregates (snapshot before save).</item>
    ///   <item>Persist changes to the database via <c>base.SaveChangesAsync</c>.</item>
    ///   <item>Dispatch each domain event through MediatR (only on successful save).</item>
    ///   <item>Clear domain events from aggregates after successful dispatch.</item>
    /// </list>
    ///
    /// <para>If the database save fails, events are NOT dispatched — the aggregate retains
    /// its events so callers can observe the failure without phantom side effects.</para>
    /// <para>Event handlers MUST be idempotent: if a handler throws after partial dispatch,
    /// the next retry re-dispatches all events for that aggregate.</para>
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Snapshot events BEFORE save — if save fails we dispatch nothing
        var aggregatesWithEvents = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // 2. Persist to database — throws on failure, events remain on aggregates
        var result = await base.SaveChangesAsync(cancellationToken);

        // 3. Dispatch domain events through MediatR only after successful save
        if (_publisher is not null && domainEvents.Count > 0)
        {
            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }

        // 4. Clear events only after successful dispatch
        aggregatesWithEvents.ForEach(a => a.ClearDomainEvents());

        return result;
    }

    /// <inheritdoc />
    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    /// Calls base SaveChangesAsync directly — no domain event dispatch — safe to call from event handlers.
    Task<int> IUnitOfWork.SaveAuditAsync(CancellationToken cancellationToken) => base.SaveChangesAsync(cancellationToken);
}
