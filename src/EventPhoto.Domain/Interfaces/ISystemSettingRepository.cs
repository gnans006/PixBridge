using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>
/// Repository contract for <see cref="SystemSetting"/> entities.
/// </summary>
public interface ISystemSettingRepository
{
    /// <summary>
    /// Gets a system setting by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching setting, or <see langword="null"/> if not found.</returns>
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all system settings.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The full settings collection.</returns>
    Task<List<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new system setting.
    /// </summary>
    /// <param name="setting">The setting to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(SystemSetting setting, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing system setting.
    /// </summary>
    /// <param name="setting">The setting to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(SystemSetting setting, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a setting value by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The setting value, or <see langword="null"/> if not found.</returns>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
}
