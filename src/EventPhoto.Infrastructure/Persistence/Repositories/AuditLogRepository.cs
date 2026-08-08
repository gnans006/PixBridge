using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(AppDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default)
        => await context.AuditLogs.AddAsync(entry, cancellationToken);

    public Task<List<AuditLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
        => context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<List<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
        => context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

    public Task<List<AuditLog>> GetByUserAsync(Guid userId, int count = 50, CancellationToken cancellationToken = default)
        => context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<(List<AuditLog> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? entityType, string? action,
        CancellationToken cancellationToken = default)
    {
        var query = context.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(action))     query = query.Where(a => a.Action == action);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
