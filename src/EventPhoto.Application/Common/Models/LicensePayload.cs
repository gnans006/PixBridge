using EventPhoto.Domain.Enums;

namespace EventPhoto.Application.Common.Models;

/// <summary>
/// Decoded payload embedded inside a PixBridge license key.
/// Extracted by <see cref="Interfaces.ILicenseKeyService.ValidateAndDecode"/> after HMAC verification.
/// </summary>
public sealed record LicensePayload
{
    /// <summary>Unique license identifier (for support reference).</summary>
    public Guid LicenseId { get; init; }

    /// <summary>Subscription plan granted by this license.</summary>
    public SubscriptionPlan Plan { get; init; }

    /// <summary>Total licensed duration in calendar days.</summary>
    public int DurationDays { get; init; }

    /// <summary>UTC timestamp when this license key was generated.</summary>
    public DateTimeOffset IssuedAtUtc { get; init; }

    /// <summary>Studio email this license was issued to.</summary>
    public string IssuedTo { get; init; } = string.Empty;

    /// <summary>
    /// Optional: if set, this license is bound to a specific installation ID.
    /// Activation on a different installation will fail HMAC binding check.
    /// </summary>
    public Guid? BoundInstallationId { get; init; }

    /// <summary>Signature version for future algorithm migration.</summary>
    public int SignatureVersion { get; init; } = 1;
}
