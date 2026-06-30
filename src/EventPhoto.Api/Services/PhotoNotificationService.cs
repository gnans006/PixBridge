using EventPhoto.Api.Hubs;
using EventPhoto.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EventPhoto.Api.Services;

/// <summary>
/// SignalR-backed implementation of <see cref="IPhotoNotificationService"/>.
/// </summary>
public sealed class PhotoNotificationService : IPhotoNotificationService
{
    private readonly IHubContext<PhotoHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhotoNotificationService"/> class.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context used for broadcasts.</param>
    public PhotoNotificationService(IHubContext<PhotoHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task NotifyPhotoAddedAsync(Guid eventId, Guid photoId, string fileName, string thumbnailUrl, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(PhotoHub.GetEventGroupName(eventId.ToString())).SendAsync(
            "photo:new",
            new
            {
                photoId,
                eventId,
                fileName,
                thumbnailUrl,
                capturedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task NotifyPhotoDeletedAsync(Guid eventId, Guid photoId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(PhotoHub.GetEventGroupName(eventId.ToString())).SendAsync(
            "photo:deleted",
            new { photoId, eventId },
            cancellationToken);
    }
}
