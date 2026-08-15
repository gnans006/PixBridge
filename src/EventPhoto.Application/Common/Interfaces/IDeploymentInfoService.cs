namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Classifies how PixBridge is currently deployed and accessible.
/// Determined by analysing <c>ApplicationSettings.PublicBaseUrl</c>.
/// </summary>
public enum DeploymentMode
{
    /// <summary>Bound to localhost only — development or single-machine use.</summary>
    Localhost,

    /// <summary>Private LAN IP — guests must be on the same network/Wi-Fi.</summary>
    Lan,

    /// <summary>Public IP via router port-forwarding — guests access via ISP-assigned public IP.</summary>
    Router,

    /// <summary>Custom domain name — production deployment with DNS and optional TLS.</summary>
    Domain,
}

/// <summary>
/// Snapshot of the active deployment topology derived from <c>PublicBaseUrl</c>
/// and optional inbound request headers.
/// </summary>
public sealed record DeploymentStatus(
    DeploymentMode Mode,
    string PublicBaseUrl,
    bool IsHttps,
    bool HasExplicitPort,
    bool IsReverseProxyDetected,
    string? DetectedProxy,
    string ModeLabel,
    string ModeDescription,
    bool IsInternetAccessible,
    bool HttpsWarning);

/// <summary>
/// Analyses the configured <c>PublicBaseUrl</c> and optional inbound request headers
/// to determine the active deployment mode and reverse-proxy topology.
/// </summary>
public interface IDeploymentInfoService
{
    /// <summary>
    /// Returns the deployment status for the given public base URL.
    /// </summary>
    /// <param name="publicBaseUrl">The value from <c>ApplicationSettings.PublicBaseUrl</c>.</param>
    /// <param name="requestHeaders">
    /// Optional set of inbound request header key-value pairs used for proxy detection.
    /// Pass <see langword="null"/> when called outside an HTTP request context.
    /// </param>
    DeploymentStatus Analyze(
        string publicBaseUrl,
        IEnumerable<KeyValuePair<string, string>>? requestHeaders = null);
}
