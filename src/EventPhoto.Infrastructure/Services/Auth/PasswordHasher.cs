using EventPhoto.Application.Common.Interfaces;

namespace EventPhoto.Infrastructure.Services.Auth;

/// <summary>BCrypt-based implementation of <see cref="IPasswordHasher"/>.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    /// <inheritdoc />
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
