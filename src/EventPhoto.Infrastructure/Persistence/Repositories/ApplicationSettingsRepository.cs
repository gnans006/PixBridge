using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IApplicationSettingsRepository"/>.
/// Enforces the single-row pattern using the well-known <see cref="ApplicationSettings.SingletonId"/>.
/// </summary>
public sealed class ApplicationSettingsRepository(AppDbContext context)
    : IApplicationSettingsRepository
{
    /// <inheritdoc />
    public Task<ApplicationSettings?> GetAsync(CancellationToken cancellationToken = default)
        => context.ApplicationSettings
            .FirstOrDefaultAsync(
                a => a.Id == ApplicationSettings.SingletonId,
                cancellationToken);

    /// <inheritdoc />
    public async Task<ApplicationSettings> GetOrCreateDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var defaults = ApplicationSettings.CreateDefault();
        await context.ApplicationSettings.AddAsync(defaults, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return defaults;
    }

    /// <inheritdoc />
    public async Task AddAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default)
        => await context.ApplicationSettings.AddAsync(settings, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default)
    {
        context.ApplicationSettings.Update(settings);
        return Task.CompletedTask;
    }
}
