using EventPhoto.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Worker.Services;

/// <summary>
/// No-op implementation of <see cref="IFaceNotificationService"/> for the worker process.
/// SignalR notifications are handled by the API process; the worker only needs a no-op stub.
/// </summary>
public sealed class NoOpFaceNotificationService(ILogger<NoOpFaceNotificationService> logger)
    : IFaceNotificationService
{
    /// <inheritdoc />
    public Task NotifySearchStartedAsync(string sessionToken, Guid eventId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Face search-started notification skipped in worker for session {Token}.", sessionToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task NotifySearchProgressAsync(string sessionToken, int matchCount, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Face search-progress notification skipped in worker for session {Token}.", sessionToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task NotifySearchCompletedAsync(string sessionToken, int matchCount, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Face search-completed notification skipped in worker for session {Token}.", sessionToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task NotifyFaceIndexCompletedAsync(Guid eventId, Guid photoId, int faceCount, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Face index-completed notification skipped in worker for photo {PhotoId}.", photoId);
        return Task.CompletedTask;
    }
}
