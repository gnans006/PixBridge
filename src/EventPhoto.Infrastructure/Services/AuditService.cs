using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;

namespace EventPhoto.Infrastructure.Services;

/// <summary>Writes audit entries through the repository and saves immediately.</summary>
public sealed class AuditService(IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork) : IAuditService
{
    public async Task LogAsync(
        string action,
        string entityType,
        string description,
        Guid? userId = null,
        string? actorName = null,
        string? entityId = null,
        CancellationToken cancellationToken = default)
    {
        var entry = AuditLog.Create(action, entityType, description, userId, actorName, entityId);
        await auditLogRepository.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
