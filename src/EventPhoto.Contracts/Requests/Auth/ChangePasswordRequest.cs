namespace EventPhoto.Contracts.Requests.Auth;

/// <summary>
/// Request body for changing a user's password.
/// </summary>
/// <param name="CurrentPassword">The current plain-text password for verification.</param>
/// <param name="NewPassword">The new plain-text password to set.</param>
/// <param name="ConfirmNewPassword">The confirmation value for the new password.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
