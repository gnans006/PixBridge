using EventPhoto.Domain.Entities;

namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Service for recording audit trail entries from Application handlers.
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        string description,
        Guid? userId = null,
        string? actorName = null,
        string? entityId = null,
        CancellationToken cancellationToken = default);
}
