namespace EventPhoto.Application.Common.Interfaces;

/// <summary>Contract for password hashing and verification.</summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plain-text password.</summary>
    string Hash(string password);

    /// <summary>Verifies a plain-text password against a stored hash.</summary>
    bool Verify(string password, string hash);

    /// <summary>Backward-compatible alias for <see cref="Hash(string)"/>.</summary>
    string HashPassword(string password) => Hash(password);

    /// <summary>Backward-compatible alias for <see cref="Verify(string, string)"/>.</summary>
    bool VerifyPassword(string password, string hash) => Verify(password, hash);
}
