namespace EventPhoto.Contracts.Requests.Users;

/// <summary>Request payload for updating an existing studio user account.</summary>
public sealed record UpdateStudioUserRequest(
    string FullName,
    string Email,
    string? Phone,
    string Role);
