using EventPhoto.Application.Common.Interfaces;

namespace EventPhoto.Infrastructure.Services;

/// <summary>
/// BCrypt-based implementation of <see cref="IPasswordService"/>.
/// </summary>
public sealed class PasswordService : IPasswordService
{
    /// <inheritdoc />
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    /// <inheritdoc />
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
