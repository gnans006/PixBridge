namespace EventPhoto.Contracts.Responses.Users;

/// <summary>Studio user profile returned by the API.</summary>
public sealed record StudioUserResponse(
    Guid Id,
    string FullName,
    string Username,
    string Email,
    string? Phone,
    string Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
