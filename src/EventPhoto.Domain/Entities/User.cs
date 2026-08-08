using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Represents a studio staff member who can manage events and photos.
/// </summary>
public sealed class User : Entity
{
    private User()
    {
    }

    /// <summary>
    /// Gets the username used for login.
    /// </summary>
    public string Username { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the bcrypt hashed password.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the user role.
    /// </summary>
    public UserRole Role { get; private set; } = UserRole.Admin;

    /// <summary>
    /// Gets a value indicating whether the account is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets the last login timestamp.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>
    /// Gets the full name of the studio staff member.
    /// </summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the display name used in the UI and JWT claims. Falls back to username.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? Username : FullName;

    /// <summary>
    /// Gets the optional phone number.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="email">The email address.</param>
    /// <param name="passwordHash">The hashed password.</param>
    /// <param name="role">The application role.</param>
    /// <returns>A new <see cref="User"/> instance.</returns>
    public static User Create(string username, string email, string passwordHash, UserRole role = UserRole.StudioOwner, string? fullName = null, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        return new User
        {
            Username = username.Trim().ToLowerInvariant(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            FullName = fullName?.Trim() ?? string.Empty,
            Phone = phone?.Trim()
        };
    }

    /// <summary>
    /// Records a successful login.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Updates the password hash.
    /// </summary>
    /// <param name="newPasswordHash">The new hashed password.</param>
    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = newPasswordHash;
        Touch();
    }

    /// <summary>
    /// Deactivates this user account.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Activates this user account.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
    }

    /// <summary>
    /// Updates the user profile fields.
    /// </summary>
    public void Update(string fullName, string email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        FullName = fullName?.Trim() ?? string.Empty;
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        Touch();
    }

    /// <summary>
    /// Changes the user's role. The last StudioOwner cannot be demoted.
    /// </summary>
    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
        Touch();
    }
}

