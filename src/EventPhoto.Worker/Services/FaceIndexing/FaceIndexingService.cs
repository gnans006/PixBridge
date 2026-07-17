using EventPhoto.Application.FaceSearch.Commands;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Worker.Services.FaceIndexing;

/// <summary>
/// Background service that picks up photos with <c>FaceIndexStatus=Pending</c> in batches
/// and dispatches <see cref="ProcessFaceIndexCommand"/> for each.
///
/// Design guarantees:
/// — Never blocks FileWatcher or ThumbnailProcessor.
/// — Gallery visibility is already achieved before this service runs.
/// — Retries up to 3 times per photo; marks as Failed after exhaustion.
/// — Session expiry cleanup runs every hour.
/// </summary>
public sealed class FaceIndexingService : BackgroundService
{
    private const int BatchSize = 5;
    private const int IndexIntervalSeconds = 10;
    private const int ExpiryCleanupIntervalMinutes = 60;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FaceIndexingService> _logger;
    private DateTimeOffset _lastExpiryCleanup = DateTimeOffset.MinValue;

    /// <summary>Initializes a new instance of the <see cref="FaceIndexingService"/> class.</summary>
    public FaceIndexingService(
        IServiceScopeFactory scopeFactory,
        ILogger<FaceIndexingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FaceIndexingService starting.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(IndexIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessBatchAsync(stoppingToken);
            await CleanupExpiredSessionsIfDueAsync(stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var photoRepository = scope.ServiceProvider.GetRequiredService<IPhotoRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        List<Domain.Entities.Photo> pendingPhotos;
        try
        {
            pendingPhotos = await photoRepository.GetPendingFaceIndexAsync(BatchSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query pending face-index photos.");
            return;
        }

        if (pendingPhotos.Count == 0)
            return;

        _logger.LogInformation("FaceIndexingService: processing {Count} photo(s).", pendingPhotos.Count);

        foreach (var photo in pendingPhotos)
        {
            try
            {
                var result = await mediator.Send(new ProcessFaceIndexCommand(photo.Id), cancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "ProcessFaceIndexCommand failed for photo {PhotoId}: {Error}",
                        photo.Id, result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing face index for photo {PhotoId}.", photo.Id);
            }
        }
    }

    private async Task CleanupExpiredSessionsIfDueAsync(CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow - _lastExpiryCleanup < TimeSpan.FromMinutes(ExpiryCleanupIntervalMinutes))
            return;

        _lastExpiryCleanup = DateTimeOffset.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            var result = await mediator.Send(new ExpireFaceSessionsCommand(), cancellationToken);
            if (result.IsSuccess && result.Value > 0)
            {
                _logger.LogInformation("Expired {Count} guest face session(s).", result.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during face session expiry cleanup.");
        }
    }
}
