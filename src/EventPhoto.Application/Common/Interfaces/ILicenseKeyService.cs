using EventPhoto.Application.Common.Models;

namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// HMAC-SHA256 license key generation and validation service.
///
/// <para>Key format: <c>PXBR-{Version}-{Base64Url(payload)}-{Base64Url(HMAC)}</c></para>
///
/// <para>The signing key is stored ONLY in the server configuration via environment variable
/// <c>PIXBRIDGE_LICENSE_SIGNING_KEY</c>. It is never committed to source control.</para>
///
/// <para>Consumers must treat a <see langword="null"/> return from
/// <see cref="ValidateAndDecode"/> as a hard rejection — do not fall through.</para>
/// </summary>
public interface ILicenseKeyService
{
    /// <summary>
    /// Generates a new signed license key string from the given payload.
    /// For internal/admin tooling use only — never exposed to end-users.
    /// </summary>
    string GenerateLicense(LicensePayload payload);

    /// <summary>
    /// Validates the HMAC signature, optional installation binding, and returns the
    /// decoded payload. Returns <see langword="null"/> if the key is invalid, tampered,
    /// expired (issued in the future), or bound to a different installation.
    /// </summary>
    LicensePayload? ValidateAndDecode(string licenseKey, Guid? installationId = null);

    /// <summary>
    /// Decodes the payload WITHOUT signature validation.
    /// Use ONLY for display or diagnostics — never for enforcement decisions.
    /// </summary>
    LicensePayload? DecodeOnly(string licenseKey);
}
