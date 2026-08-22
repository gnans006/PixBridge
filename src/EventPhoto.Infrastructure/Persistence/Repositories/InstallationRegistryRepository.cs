using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>PostgreSQL-backed implementation of <see cref="IInstallationRegistryRepository"/>.</summary>
public sealed class InstallationRegistryRepository(AppDbContext context) : IInstallationRegistryRepository
{
    public Task<InstallationRegistry?> GetAsync(CancellationToken ct = default)
        => context.InstallationRegistries
            .FirstOrDefaultAsync(r => r.Id == InstallationRegistry.SingletonId, ct);

    public async Task AddAsync(InstallationRegistry registry, CancellationToken ct = default)
        => await context.InstallationRegistries.AddAsync(registry, ct);

    public Task UpdateAsync(InstallationRegistry registry, CancellationToken ct = default)
    {
        context.InstallationRegistries.Update(registry);
        return Task.CompletedTask;
    }
}
