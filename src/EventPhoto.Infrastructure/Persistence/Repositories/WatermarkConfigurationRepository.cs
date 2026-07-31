using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IWatermarkConfigurationRepository"/>.
/// </summary>
public sealed class WatermarkConfigurationRepository(AppDbContext context)
    : IWatermarkConfigurationRepository
{
    /// <inheritdoc />
    public Task<WatermarkConfiguration?> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
        => context.WatermarkConfigurations
            .FirstOrDefaultAsync(w => w.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(
        WatermarkConfiguration config,
        CancellationToken cancellationToken = default)
        => await context.WatermarkConfigurations.AddAsync(config, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(
        WatermarkConfiguration config,
        CancellationToken cancellationToken = default)
    {
        context.WatermarkConfigurations.Update(config);
        return Task.CompletedTask;
    }
}
