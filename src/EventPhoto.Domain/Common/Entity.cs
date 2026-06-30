namespace EventPhoto.Domain.Common;

/// <summary>
/// Base entity with UUID primary key and audit timestamps.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the UTC timestamp when the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the UTC timestamp when the entity was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Marks the entity as updated at the current UTC time.
    /// </summary>
    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
