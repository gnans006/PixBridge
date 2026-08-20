using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Events;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Photos.EventHandlers;

/// <summary>
/// Writes an audit log entry when a new photo is detected and registered.
/// Runs asynchronously after the photo record is persisted — gallery visibility is not blocked.
/// </summary>
public sealed class PhotoCreatedAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<PhotoCreatedAuditHandler> logger)
    : INotificationHandler<PhotoCreatedEvent>
{
    public async Task Handle(PhotoCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var entry = AuditLog.Create(
                action: AuditAction.Created,
                entityType: "Photo",
                description: $"Photo '{notification.FileName}' was registered for event {notification.EventId}.",
                entityId: notification.PhotoId.ToString());

            await auditLogRepository.AddAsync(entry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log for PhotoCreated (PhotoId={PhotoId}).", notification.PhotoId);
        }
    }
}
