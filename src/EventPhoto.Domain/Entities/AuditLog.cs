using EventPhoto.Domain.Common;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Immutable audit trail record for significant platform actions.
/// </summary>
public sealed class AuditLog : Entity
{
    private AuditLog() { }

    /// <summary>Gets the identifier of the user who performed the action (null for system/anonymous).</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Gets the display name of the actor at the time of the action.</summary>
    public string ActorName { get; private set; } = string.Empty;

    /// <summary>Gets the entity type affected (e.g. "Event", "User", "Settings").</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Gets the identifier of the affected entity (null for non-entity actions).</summary>
    public string? EntityId { get; private set; }

    /// <summary>Gets the action performed (e.g. "Created", "Updated", "Deleted", "Login").</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable description of what changed.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp of the action.</summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>Creates a new audit log entry.</summary>
    public static AuditLog Create(
        string action,
        string entityType,
        string description,
        Guid? userId = null,
        string? actorName = null,
        string? entityId = null)
    {
        return new AuditLog
        {
            UserId = userId,
            ActorName = actorName ?? "System",
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Description = description,
            Timestamp = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>Well-known audit action constants.</summary>
public static class AuditAction
{
    public const string Login           = "Login";
    public const string Logout          = "Logout";
    public const string Created         = "Created";
    public const string Updated         = "Updated";
    public const string Deleted         = "Deleted";
    public const string Activated       = "Activated";
    public const string Deactivated     = "Deactivated";
    public const string RoleChanged     = "RoleChanged";
    public const string PasswordReset   = "PasswordReset";
    public const string QrGenerated     = "QrGenerated";
    public const string SettingsUpdated = "SettingsUpdated";
}
