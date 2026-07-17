namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Contract for broadcasting face-recognition real-time events to guests via SignalR.
/// </summary>
public interface IFaceNotificationService
{
    /// <summary>
    /// Notifies a specific guest session that their face search has started.
    /// Sends <c>face-search-started</c> to the session's private group.
    /// </summary>
    Task NotifySearchStartedAsync(string sessionToken, Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a progress update to the session's private group.
    /// Sends <c>face-search-progress</c> with the current match count.
    /// </summary>
    Task NotifySearchProgressAsync(string sessionToken, int matchCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the session that face search completed successfully.
    /// Sends <c>face-search-completed</c> with total match count.
    /// </summary>
    Task NotifySearchCompletedAsync(string sessionToken, int matchCount, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts to all gallery clients for an event that a photo has been face-indexed.
    /// Sends <c>face-index-completed</c> to the event group.
    /// </summary>
    Task NotifyFaceIndexCompletedAsync(Guid eventId, Guid photoId, int faceCount, CancellationToken cancellationToken = default);
}
