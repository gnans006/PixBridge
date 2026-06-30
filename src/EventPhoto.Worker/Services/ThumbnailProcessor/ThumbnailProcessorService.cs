using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Worker.Services.ThumbnailProcessor;

/// <summary>
/// Background service that generates thumbnails for photos awaiting processing.
/// </summary>
public sealed class ThumbnailProcessorService : BackgroundService
{
    private const int BatchSize = 10;
    private const int IntervalSeconds = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ThumbnailProcessorService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailProcessorService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="logger">The logger instance.</param>
    public ThumbnailProcessorService(IServiceScopeFactory scopeFactory, ILogger<ThumbnailProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Thumbnail processor service starting.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(IntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessBatchAsync(stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var photoRepository = scope.ServiceProvider.GetRequiredService<IPhotoRepository>();
        var thumbnailService = scope.ServiceProvider.GetRequiredService<IThumbnailService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IPhotoNotificationService>();
        var settingRepository = scope.ServiceProvider.GetRequiredService<ISystemSettingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var maxWidth = int.TryParse(await settingRepository.GetValueAsync("thumbnail.width", cancellationToken), out var widthValue) ? widthValue : 400;
        var maxHeight = int.TryParse(await settingRepository.GetValueAsync("thumbnail.height", cancellationToken), out var heightValue) ? heightValue : 400;
        var quality = int.TryParse(await settingRepository.GetValueAsync("thumbnail.quality", cancellationToken), out var qualityValue) ? qualityValue : 85;
        var serverUrl = await settingRepository.GetValueAsync("app.serverUrl", cancellationToken) ?? "http://192.168.10.10";

        var pendingPhotos = await photoRepository.GetPendingThumbnailsAsync(BatchSize, cancellationToken);
        if (pendingPhotos.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} thumbnail jobs.", pendingPhotos.Count);

        foreach (var photo in pendingPhotos)
        {
            try
            {
                photo.MarkThumbnailProcessing();
                await photoRepository.UpdateAsync(photo, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                var (width, height) = await thumbnailService.GenerateAsync(
                    photo.OriginalPath,
                    photo.ThumbnailPath,
                    maxWidth,
                    maxHeight,
                    quality,
                    cancellationToken);

                photo.MarkThumbnailDone(width, height);
                await photoRepository.UpdateAsync(photo, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                var thumbnailUrl = $"{serverUrl.TrimEnd('/')}/api/photos/{photo.Id}/thumbnail";
                await notificationService.NotifyPhotoAddedAsync(photo.EventId, photo.Id, photo.FileName, thumbnailUrl, cancellationToken);

                _logger.LogInformation("Generated thumbnail for photo {PhotoId}.", photo.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate thumbnail for photo {PhotoId}.", photo.Id);
                photo.MarkThumbnailFailed();
                await photoRepository.UpdateAsync(photo, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
