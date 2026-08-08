using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for <see cref="AuditLog"/> entries.</summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetByUserAsync(Guid userId, int count = 50, CancellationToken cancellationToken = default);
    Task<(List<AuditLog> Items, int Total)> GetPagedAsync(int page, int pageSize, string? entityType, string? action, CancellationToken cancellationToken = default);
}
