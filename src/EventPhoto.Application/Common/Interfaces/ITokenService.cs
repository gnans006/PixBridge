using EventPhoto.Domain.Entities;

namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Service contract for generating and validating JWT bearer tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT bearer token for the given user.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The signed JWT string.</returns>
    string GenerateToken(User user);

    /// <summary>
    /// Returns the token expiry as a UTC timestamp for the given user's token.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The UTC <see cref="DateTimeOffset"/> when the token expires.</returns>
    DateTimeOffset GetTokenExpiry(User user);
}
