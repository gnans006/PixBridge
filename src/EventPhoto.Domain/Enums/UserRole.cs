namespace EventPhoto.Domain.Enums;

/// <summary>
/// Application user roles. Admin and Viewer are legacy values kept for backward compatibility.
/// New deployments use StudioOwner, StudioManager, and Operator.
/// </summary>
public enum UserRole
{
    // ── Legacy (backward-compatible) ─────────────────────────────────────────

    /// <summary>Legacy: full-access administrator. Treated as StudioOwner.</summary>
    Admin = 1,

    /// <summary>Legacy: read-only viewer. Treated as Operator.</summary>
    Viewer = 2,

    // ── Studio Business Roles ────────────────────────────────────────────────

    /// <summary>Full platform control — billing, users, branding, all events.</summary>
    StudioOwner = 10,

    /// <summary>Operational manager — events, QR, analytics. No user/platform management.</summary>
    StudioManager = 20,

    /// <summary>Operational staff — create/edit events and QR only.</summary>
    Operator = 30,
}

/// <summary>
/// Extension methods for role resolution and backward-compatibility mapping.
/// </summary>
public static class UserRoleExtensions
{
    /// <summary>
    /// Returns the canonical role name used in JWT claims and policy evaluation.
    /// Maps legacy Admin→StudioOwner and Viewer→Operator.
    /// </summary>
    public static string ToClaimValue(this UserRole role) => role switch
    {
        UserRole.Admin        => nameof(UserRole.StudioOwner),
        UserRole.Viewer       => nameof(UserRole.Operator),
        UserRole.StudioOwner  => nameof(UserRole.StudioOwner),
        UserRole.StudioManager => nameof(UserRole.StudioManager),
        UserRole.Operator     => nameof(UserRole.Operator),
        _                     => role.ToString()
    };

    /// <summary>Returns true when this role has owner-level access.</summary>
    public static bool IsOwner(this UserRole role) =>
        role is UserRole.StudioOwner or UserRole.Admin;

    /// <summary>Returns true when this role has manager-level access or above.</summary>
    public static bool IsManagerOrAbove(this UserRole role) =>
        role is UserRole.StudioOwner or UserRole.Admin or UserRole.StudioManager;

    /// <summary>Returns true for any authenticated studio role.</summary>
    public static bool IsOperatorOrAbove(this UserRole role) =>
        role is UserRole.StudioOwner or UserRole.Admin
            or UserRole.StudioManager
            or UserRole.Operator or UserRole.Viewer;
}

