namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Service contract for hashing and verifying passwords.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Computes a secure hash for the supplied plain-text password.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The hashed password string.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a plain-text password against a stored hash.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="hash">The stored password hash.</param>
    /// <returns><see langword="true"/> when the password matches the hash; otherwise <see langword="false"/>.</returns>
    bool Verify(string password, string hash);
}
