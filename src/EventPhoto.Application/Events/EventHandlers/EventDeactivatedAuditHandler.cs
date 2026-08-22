using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Events;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Events.EventHandlers;

/// <summary>
/// Writes an audit log entry when a photography event is deactivated.
/// </summary>
public sealed class EventDeactivatedAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<EventDeactivatedAuditHandler> logger)
    : INotificationHandler<EventDeactivatedEvent>
{
    public async Task Handle(EventDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var entry = AuditLog.Create(
                action: AuditAction.Deactivated,
                entityType: "Event",
                description: "Event was deactivated — no longer accepting new photos.",
                entityId: notification.EventId.ToString());

            await auditLogRepository.AddAsync(entry, cancellationToken);
            await unitOfWork.SaveAuditAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log for EventDeactivated (EventId={EventId}).", notification.EventId);
        }
    }
}
