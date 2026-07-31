using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>
/// Repository contract for <see cref="WatermarkConfiguration"/> entities.
/// </summary>
public interface IWatermarkConfigurationRepository
{
    /// <summary>
    /// Gets the watermark configuration for the specified event, or
    /// <see langword="null"/> when no configuration has been saved yet.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<WatermarkConfiguration?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new watermark configuration.</summary>
    /// <param name="config">The configuration to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(WatermarkConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>Marks an existing watermark configuration as modified.</summary>
    /// <param name="config">The configuration to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(WatermarkConfiguration config, CancellationToken cancellationToken = default);
}
