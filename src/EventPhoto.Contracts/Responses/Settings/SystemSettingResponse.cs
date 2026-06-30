namespace EventPhoto.Contracts.Responses.Settings;

/// <summary>
/// Response for a single application-level system setting.
/// </summary>
/// <param name="Id">The setting identifier.</param>
/// <param name="Key">The unique setting key.</param>
/// <param name="Value">The setting value stored as a string.</param>
/// <param name="Description">The optional human-readable description.</param>
public sealed record SystemSettingResponse(
    Guid Id,
    string Key,
    string Value,
    string? Description);
