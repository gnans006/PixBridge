namespace EventPhoto.Application.Common.Interfaces;

/// <summary>Contract for broadcasting real-time photo notifications to connected gallery clients.</summary>
public interface IPhotoNotificationService
{
    /// <summary>Broadcasts a new photo notification to all gallery clients watching the given event.</summary>
    Task NotifyPhotoAddedAsync(Guid eventId, Guid photoId, string fileName, string thumbnailUrl, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts a photo deleted notification.</summary>
    Task NotifyPhotoDeletedAsync(Guid eventId, Guid photoId, CancellationToken cancellationToken = default);
}
