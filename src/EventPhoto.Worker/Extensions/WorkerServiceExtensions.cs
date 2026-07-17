using EventPhoto.Worker.Services.FaceIndexing;
using EventPhoto.Worker.Services.FileWatcher;
using EventPhoto.Worker.Services.ThumbnailProcessor;

namespace EventPhoto.Worker.Extensions;

/// <summary>
/// Extension methods for registering PixBridge worker hosted services.
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
        services.AddHostedService<FileWatcherService>();
        services.AddHostedService<ThumbnailProcessorService>();
        services.AddHostedService<FaceIndexingService>();
        return services;
    }
}
