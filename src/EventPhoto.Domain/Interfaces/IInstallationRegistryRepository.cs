using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for the <see cref="InstallationRegistry"/> singleton.</summary>
public interface IInstallationRegistryRepository
{
    /// <summary>Returns the installation registry record, or <see langword="null"/> if not yet seeded.</summary>
    Task<InstallationRegistry?> GetAsync(CancellationToken ct = default);

    /// <summary>Inserts the installation record on first boot.</summary>
    Task AddAsync(InstallationRegistry registry, CancellationToken ct = default);

    /// <summary>Persists changes to an existing installation record.</summary>
    Task UpdateAsync(InstallationRegistry registry, CancellationToken ct = default);
}
