using EventPhoto.Domain.Common;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Application-wide configuration setting stored in the database.
/// </summary>
public sealed class SystemSetting : Entity
{
    private SystemSetting()
    {
    }

    /// <summary>
    /// Gets the setting key.
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the setting value stored as a string.
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional human-readable description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Creates a new system setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="description">The optional description.</param>
    /// <returns>A new <see cref="SystemSetting"/> instance.</returns>
    public static SystemSetting Create(string key, string value, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Setting key is required.");
        }

        if (value is null)
        {
            throw new DomainException("Setting value is required.");
        }

        return new SystemSetting
        {
            Key = key.Trim(),
            Value = value,
            Description = description?.Trim()
        };
    }

    /// <summary>
    /// Updates the setting value.
    /// </summary>
    /// <param name="value">The updated value.</param>
    public void UpdateValue(string value)
    {
        if (value is null)
        {
            throw new DomainException("Setting value is required.");
        }

        Value = value;
        Touch();
    }
}
