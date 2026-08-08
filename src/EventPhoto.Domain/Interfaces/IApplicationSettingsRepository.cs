using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for the <see cref="ApplicationSettings"/> singleton aggregate.</summary>
public interface IApplicationSettingsRepository
{
    /// <summary>Returns the application settings, or <see langword="null"/> if not yet seeded.</summary>
    Task<ApplicationSettings?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the application settings. If no record exists, creates and persists the default
    /// record automatically (idempotent seed).
    /// </summary>
    Task<ApplicationSettings> GetOrCreateDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds the settings record for the first time.</summary>
    Task AddAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Marks the settings record as modified.</summary>
    Task UpdateAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}
