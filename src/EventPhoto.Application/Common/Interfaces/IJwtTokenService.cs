using EventPhoto.Domain.Entities;

namespace EventPhoto.Application.Common.Interfaces;

/// <summary>Contract for generating and validating JWT tokens.</summary>
public interface IJwtTokenService
{
    /// <summary>Generates a signed JWT access token for the given user.</summary>
    string GenerateToken(User user);

    /// <summary>Validates a token and returns the user ID if valid.</summary>
    Guid? ValidateToken(string token);
}
