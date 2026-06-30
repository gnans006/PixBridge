using EventPhoto.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Worker.Services;

/// <summary>
/// No-op implementation of <see cref="IPhotoNotificationService"/> for the worker process.
/// </summary>
public sealed class NoOpPhotoNotificationService : IPhotoNotificationService
{
    private readonly ILogger<NoOpPhotoNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoOpPhotoNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public NoOpPhotoNotificationService(ILogger<NoOpPhotoNotificationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task NotifyPhotoAddedAsync(Guid eventId, Guid photoId, string fileName, string thumbnailUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Photo notification skipped in worker for photo {PhotoId}.", photoId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task NotifyPhotoDeletedAsync(Guid eventId, Guid photoId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Photo delete notification skipped in worker for photo {PhotoId}.", photoId);
        return Task.CompletedTask;
    }
}
