using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Events;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Photos.EventHandlers;

/// <summary>
/// Writes an audit log entry when a photo is soft-deleted.
/// </summary>
public sealed class PhotoDeletedAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<PhotoDeletedAuditHandler> logger)
    : INotificationHandler<PhotoDeletedEvent>
{
    public async Task Handle(PhotoDeletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var entry = AuditLog.Create(
                action: AuditAction.Deleted,
                entityType: "Photo",
                description: $"Photo was deleted from event {notification.EventId}.",
                entityId: notification.PhotoId.ToString());

            await auditLogRepository.AddAsync(entry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log for PhotoDeleted (PhotoId={PhotoId}).", notification.PhotoId);
        }
    }
}
