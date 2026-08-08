namespace EventPhoto.Contracts.Requests.Users;

/// <summary>Request payload for creating a new studio user account.</summary>
public sealed record CreateStudioUserRequest(
    string FullName,
    string Username,
    string Email,
    string? Phone,
    string Role,
    string Password,
    string ConfirmPassword);
