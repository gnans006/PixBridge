namespace EventPhoto.Contracts.Requests.Users;

/// <summary>Request payload for resetting a studio user's password (owner action).</summary>
public sealed record ResetUserPasswordRequest(string NewPassword, string ConfirmPassword);
