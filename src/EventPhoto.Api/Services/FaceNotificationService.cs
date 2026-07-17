using EventPhoto.Api.Hubs;
using EventPhoto.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EventPhoto.Api.Services;

/// <summary>
/// SignalR-backed implementation of <see cref="IFaceNotificationService"/>.
/// Sends face-recognition lifecycle events to the appropriate client groups.
/// </summary>
public sealed class FaceNotificationService(IHubContext<PhotoHub> hubContext)
    : IFaceNotificationService
{
    // Guest sessions join a private group named after their session token
    private static string SessionGroup(string token) => $"face-session-{token}";

    /// <inheritdoc />
    public Task NotifySearchStartedAsync(
        string sessionToken,
        Guid eventId,
        CancellationToken cancellationToken = default)
        => hubContext.Clients
            .Group(SessionGroup(sessionToken))
            .SendAsync(
                "face-search-started",
                new { sessionToken, eventId, startedAt = DateTimeOffset.UtcNow },
                cancellationToken);

    /// <inheritdoc />
    public Task NotifySearchProgressAsync(
        string sessionToken,
        int matchCount,
        CancellationToken cancellationToken = default)
        => hubContext.Clients
            .Group(SessionGroup(sessionToken))
            .SendAsync(
                "face-search-progress",
                new { sessionToken, matchCount },
                cancellationToken);

    /// <inheritdoc />
    public Task NotifySearchCompletedAsync(
        string sessionToken,
        int matchCount,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
        => hubContext.Clients
            .Group(SessionGroup(sessionToken))
            .SendAsync(
                "face-search-completed",
                new { sessionToken, matchCount, expiresAt, completedAt = DateTimeOffset.UtcNow },
                cancellationToken);

    /// <inheritdoc />
    public Task NotifyFaceIndexCompletedAsync(
        Guid eventId,
        Guid photoId,
        int faceCount,
        CancellationToken cancellationToken = default)
        => hubContext.Clients
            .Group(PhotoHub.GetEventGroupName(eventId.ToString()))
            .SendAsync(
                "face-index-completed",
                new { eventId, photoId, faceCount, indexedAt = DateTimeOffset.UtcNow },
                cancellationToken);
}
