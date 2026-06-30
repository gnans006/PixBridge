using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="ISystemSettingRepository"/>.
/// </summary>
public sealed class SystemSettingRepository(AppDbContext context)
    : ISystemSettingRepository
{
    /// <inheritdoc />
    public Task<SystemSetting?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
        => context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

    /// <inheritdoc />
    public Task<List<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default)
        => context.SystemSettings
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(SystemSetting setting, CancellationToken cancellationToken = default)
        => await context.SystemSettings.AddAsync(setting, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(
        SystemSetting setting,
        CancellationToken cancellationToken = default)
    {
        context.SystemSettings.Update(setting);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<string?> GetValueAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetByKeyAsync(key, cancellationToken);
        return setting?.Value;
    }
}
