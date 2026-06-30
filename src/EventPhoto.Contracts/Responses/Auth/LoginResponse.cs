namespace EventPhoto.Contracts.Responses.Auth;

/// <summary>Response returned after a successful login.</summary>
/// <param name="AccessToken">The signed JWT bearer token.</param>
/// <param name="Username">The authenticated username.</param>
/// <param name="Role">The user role name.</param>
/// <param name="ExpiresAt">The UTC timestamp when the token expires.</param>
public sealed record LoginResponse(string AccessToken, string Username, string Role, DateTimeOffset ExpiresAt);
