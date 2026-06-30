namespace EventPhoto.Infrastructure.Settings;

/// <summary>
/// Configuration settings for JWT token generation.
/// Bound from the <c>Jwt</c> configuration section.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets the HMAC-SHA256 signing secret (min. 32 characters).
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token lifetime in minutes. Defaults to 480 (8 hours).
    /// </summary>
    public int ExpiryMinutes { get; set; } = 480;
}
