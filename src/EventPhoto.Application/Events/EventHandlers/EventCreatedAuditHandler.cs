using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Events;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Events.EventHandlers;

/// <summary>
/// Writes an audit log entry when a new photography event is created.
/// Triggered by <see cref="EventCreatedEvent"/> dispatched from
/// <see cref="AppDbContext.SaveChangesAsync"/> after the event is persisted.
/// </summary>
public sealed class EventCreatedAuditHandler(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<EventCreatedAuditHandler> logger)
    : INotificationHandler<EventCreatedEvent>
{
    public async Task Handle(EventCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var entry = AuditLog.Create(
                action: AuditAction.Created,
                entityType: "Event",
                description: $"Event '{notification.EventName}' was created.",
                entityId: notification.EventId.ToString());

            await auditLogRepository.AddAsync(entry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit failure must never propagate — log and continue
            logger.LogError(ex, "Failed to write audit log for EventCreated (EventId={EventId}).", notification.EventId);
        }
    }
}
