using EventPhoto.Application.Common.Interfaces;
using System.Net;

namespace EventPhoto.Infrastructure.Services.Network;

/// <summary>
/// Analyses <c>ApplicationSettings.PublicBaseUrl</c> to determine the active
/// deployment mode and detect reverse-proxy presence from request headers.
/// </summary>
public sealed class DeploymentInfoService : IDeploymentInfoService
{
    /// <inheritdoc />
    public DeploymentStatus Analyze(
        string publicBaseUrl,
        IEnumerable<KeyValuePair<string, string>>? requestHeaders = null)
    {
        if (!Uri.TryCreate(publicBaseUrl?.Trim(), UriKind.Absolute, out var uri))
        {
            return Fallback(publicBaseUrl ?? string.Empty);
        }

        var host            = uri.Host;
        var isHttps         = uri.Scheme == Uri.UriSchemeHttps;
        var hasExplicitPort = !uri.IsDefaultPort;

        var mode = ClassifyHost(host);

        var (proxyDetected, proxyName) = DetectProxy(requestHeaders);

        var (label, description, isInternet) = Describe(mode, isHttps);
        var httpsWarning = isInternet && !isHttps && !proxyDetected;

        return new DeploymentStatus(
            Mode:                    mode,
            PublicBaseUrl:           publicBaseUrl!.TrimEnd('/'),
            IsHttps:                 isHttps,
            HasExplicitPort:         hasExplicitPort,
            IsReverseProxyDetected:  proxyDetected,
            DetectedProxy:           proxyName,
            ModeLabel:               label,
            ModeDescription:         description,
            IsInternetAccessible:    isInternet,
            HttpsWarning:            httpsWarning);
    }

    // ── Classification ─────────────────────────────────────────────────────────

    private static DeploymentMode ClassifyHost(string host)
    {
        // Loopback / localhost
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host == "127.0.0.1" || host == "::1")
        {
            return DeploymentMode.Localhost;
        }

        // Numeric IP — determine private vs public
        if (IPAddress.TryParse(host, out var ip))
        {
            return IsPrivateIp(ip) ? DeploymentMode.Lan : DeploymentMode.Router;
        }

        // Hostname/domain
        return DeploymentMode.Domain;
    }

    private static bool IsPrivateIp(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4) return false;

        return bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }

    // ── Proxy detection ────────────────────────────────────────────────────────

    private static (bool detected, string? name) DetectProxy(
        IEnumerable<KeyValuePair<string, string>>? headers)
    {
        if (headers is null) return (false, null);

        var dict = headers
            .GroupBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);

        if (dict.ContainsKey("CF-Connecting-IP"))   return (true, "Cloudflare");
        if (dict.ContainsKey("X-Forwarded-Proto"))  return (true, DetectProxyBySignature(dict));
        if (dict.ContainsKey("X-Real-IP"))           return (true, "nginx");
        if (dict.ContainsKey("Forwarded"))           return (true, "RFC 7239 proxy");
        if (dict.ContainsKey("X-Forwarded-For"))     return (true, "Reverse proxy");

        return (false, null);
    }

    private static string DetectProxyBySignature(
        Dictionary<string, string> dict)
    {
        if (dict.ContainsKey("CF-Ray"))            return "Cloudflare";
        if (dict.ContainsKey("X-Real-IP"))         return "nginx";
        if (dict.ContainsKey("Caddy-Request-Id"))  return "Caddy";
        if (dict.ContainsKey("X-ARR-LOG-ID"))      return "IIS ARR";
        return "Reverse proxy";
    }

    // ── Description ────────────────────────────────────────────────────────────

    private static (string label, string description, bool isInternet) Describe(
        DeploymentMode mode,
        bool isHttps)
        => mode switch
        {
            DeploymentMode.Localhost => (
                "Local",
                "Server accessible on this machine only. Not reachable by guests.",
                false),

            DeploymentMode.Lan => (
                "LAN",
                "Server accessible to devices on the same Wi-Fi / local network.",
                false),

            DeploymentMode.Router => (
                "Router / Port Forward",
                "Server exposed to the internet via router port-forwarding on the public IP.",
                true),

            DeploymentMode.Domain => (
                isHttps ? "Domain (HTTPS)" : "Domain (HTTP)",
                isHttps
                    ? "Production deployment with custom domain and TLS encryption."
                    : "Custom domain detected. HTTPS strongly recommended for internet access.",
                true),

            _ => ("Unknown", "Unable to determine deployment mode.", false),
        };

    private static DeploymentStatus Fallback(string url) =>
        new(DeploymentMode.Localhost, url, false, false, false, null,
            "Unknown", "Invalid PublicBaseUrl — please configure a valid URL.", false, false);
}
