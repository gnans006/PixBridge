namespace EventPhoto.Contracts.Requests.Auth;

/// <summary>
/// Request body for the login endpoint.
/// </summary>
/// <param name="Username">The account username.</param>
/// <param name="Password">The plain-text password.</param>
public sealed record LoginRequest(string Username, string Password);
