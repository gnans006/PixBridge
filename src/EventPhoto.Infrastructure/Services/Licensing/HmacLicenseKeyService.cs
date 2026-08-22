using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EventPhoto.Infrastructure.Services.Licensing;

/// <summary>
/// HMAC-SHA256 license key service.
///
/// <para>Key format:</para>
/// <code>PXBR-1-{Base64Url(utf8(json_payload))}-{Base64Url(HMAC-SHA256(payload_bytes, signingKey))}</code>
///
/// <para>The signing key is read from configuration key <c>Licensing:SigningKey</c>
/// (set via environment variable <c>PIXBRIDGE_LICENSE_SIGNING_KEY</c>).
/// When the signing key is absent or empty, <see cref="GenerateLicense"/> throws and
/// <see cref="ValidateAndDecode"/> returns <see langword="null"/> (fail-closed on generation,
/// fail-open on validation for Trial-only installs).</para>
/// </summary>
public sealed class HmacLicenseKeyService(
    IConfiguration configuration,
    ILogger<HmacLicenseKeyService> logger)
    : ILicenseKeyService
{
    private const string KeyPrefix        = "PXBR";
    private const int    SupportedVersion = 1;

    // ── Public API ────────────────────────────────────────────────────────────

    public string GenerateLicense(LicensePayload payload)
    {
        var signingKey = GetSigningKeyBytes()
            ?? throw new InvalidOperationException(
                "License signing key is not configured. Set Licensing:SigningKey or PIXBRIDGE_LICENSE_SIGNING_KEY.");

        var json        = SerializePayload(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var payloadB64   = Base64UrlEncode(payloadBytes);
        var hmac         = ComputeHmac(payloadBytes, signingKey);
        var hmacB64      = Base64UrlEncode(hmac);

        return $"{KeyPrefix}-{SupportedVersion}-{payloadB64}-{hmacB64}";
    }

    public LicensePayload? ValidateAndDecode(string licenseKey, Guid? installationId = null)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return null;

        try
        {
            if (!TryParseParts(licenseKey, out var payloadBytes, out var providedHmac))
            {
                logger.LogWarning("LicenseKeyService: invalid key format.");
                return null;
            }

            // Without signing key we cannot validate — return null (not fail-open for validation)
            var signingKey = GetSigningKeyBytes();
            if (signingKey is null)
            {
                logger.LogWarning("LicenseKeyService: signing key not configured — cannot validate license.");
                return null;
            }

            // Constant-time HMAC comparison (prevents timing attacks)
            var expectedHmac = ComputeHmac(payloadBytes, signingKey);
            if (!CryptographicOperations.FixedTimeEquals(expectedHmac, providedHmac))
            {
                logger.LogWarning("LicenseKeyService: HMAC validation failed — key may be tampered.");
                return null;
            }

            var payload = DeserializePayload(payloadBytes);
            if (payload is null)
                return null;

            // Reject keys issued more than 24 h in the future (clock skew guard)
            if (payload.IssuedAtUtc > DateTimeOffset.UtcNow.AddHours(24))
            {
                logger.LogWarning("LicenseKeyService: key issued_at is more than 24h in the future.");
                return null;
            }

            // Check installation binding
            if (payload.BoundInstallationId.HasValue && installationId.HasValue
                && payload.BoundInstallationId.Value != installationId.Value)
            {
                logger.LogWarning(
                    "LicenseKeyService: key is bound to installation {Bound} but current installation is {Current}.",
                    payload.BoundInstallationId, installationId);
                return null;
            }

            return payload;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LicenseKeyService: unexpected error during validation.");
            return null;
        }
    }

    public LicensePayload? DecodeOnly(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return null;

        try
        {
            if (!TryParseParts(licenseKey, out var payloadBytes, out _))
                return null;

            return DeserializePayload(payloadBytes);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "LicenseKeyService: could not decode-only key.");
            return null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private byte[]? GetSigningKeyBytes()
    {
        var key = configuration["Licensing:SigningKey"];
        if (string.IsNullOrWhiteSpace(key))
            return null;

        return Encoding.UTF8.GetBytes(key);
    }

    private static bool TryParseParts(string licenseKey, out byte[] payloadBytes, out byte[] hmacBytes)
    {
        payloadBytes = [];
        hmacBytes    = [];

        var parts = licenseKey.Split('-', 4);
        if (parts.Length != 4)
            return false;

        if (parts[0] != KeyPrefix)
            return false;

        if (!int.TryParse(parts[1], out var version) || version != SupportedVersion)
            return false;

        try
        {
            payloadBytes = Base64UrlDecode(parts[2]);
            hmacBytes    = Base64UrlDecode(parts[3]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeHmac(byte[] data, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static string SerializePayload(LicensePayload payload)
    {
        var dto = new PayloadDto
        {
            lid  = payload.LicenseId,
            plan = payload.Plan.ToString(),
            days = payload.DurationDays,
            iat  = payload.IssuedAtUtc.ToUnixTimeSeconds(),
            ito  = payload.IssuedTo,
            iid  = payload.BoundInstallationId,
            sv   = payload.SignatureVersion,
        };
        return JsonSerializer.Serialize(dto);
    }

    private static LicensePayload? DeserializePayload(byte[] bytes)
    {
        var json = Encoding.UTF8.GetString(bytes);
        var dto  = JsonSerializer.Deserialize<PayloadDto>(json);
        if (dto is null)
            return null;

        if (!Enum.TryParse<SubscriptionPlan>(dto.plan, ignoreCase: true, out var plan))
            return null;

        return new LicensePayload
        {
            LicenseId           = dto.lid,
            Plan                = plan,
            DurationDays        = dto.days,
            IssuedAtUtc         = DateTimeOffset.FromUnixTimeSeconds(dto.iat),
            IssuedTo            = dto.ito ?? string.Empty,
            BoundInstallationId = dto.iid,
            SignatureVersion    = dto.sv,
        };
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "=";  break;
        }
        return Convert.FromBase64String(padded);
    }

    // ── Internal DTO (keep compact for key size) ──────────────────────────────

    private sealed class PayloadDto
    {
        public Guid    lid  { get; set; }
        public string? plan { get; set; }
        public int     days { get; set; }
        public long    iat  { get; set; }
        public string? ito  { get; set; }
        public Guid?   iid  { get; set; }
        public int     sv   { get; set; }
    }
}
