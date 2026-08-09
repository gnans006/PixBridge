using EventPhoto.Worker.Services.AiDiscovery;
using EventPhoto.Worker.Services.FileWatcher;
using EventPhoto.Worker.Services.ThumbnailProcessor;

namespace EventPhoto.Worker.Extensions;

/// <summary>
/// Extension methods for registering PixBridge worker hosted services.
///
/// <para>Pipeline separation:</para>
/// <list type="bullet">
///   <item><b>Gallery Pipeline</b>: FileWatcherService → ThumbnailProcessorService (highest priority)</item>
///   <item><b>AI Discovery Pipeline</b>: AiDiscoveryPipelineService (independent, never blocks gallery)</item>
///   <item><b>Maintenance</b>: DeadLetterProcessorService, SelfieRetentionService</item>
/// </list>
/// </summary>
public static class WorkerServiceExtensions
{
    /// <summary>
    /// Registers all background worker services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        // ── Gallery Pipeline (Priority 1) ─────────────────────────────────────
        services.AddHostedService<FileWatcherService>();
        services.AddHostedService<ThumbnailProcessorService>();

        // ── AI Discovery Pipeline (Priority 2) ────────────────────────────────
        services.AddHostedService<AiDiscoveryPipelineService>();

        // ── Maintenance Services ───────────────────────────────────────────────
        services.AddHostedService<DeadLetterProcessorService>();
        services.AddHostedService<SelfieRetentionService>();

        return services;
    }
}
