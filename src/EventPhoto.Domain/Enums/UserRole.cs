namespace EventPhoto.Domain.Enums;

/// <summary>
/// Application user roles.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Administrator with full access.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Read-only or limited-access viewer.
    /// </summary>
    Viewer = 2
}
