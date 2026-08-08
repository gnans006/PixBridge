namespace EventPhoto.Application.Common.Interfaces;

/// <summary>Snapshot of the current machine's network configuration.</summary>
public sealed record NetworkInformation(
    string HostName,
    string MachineName,
    string PrimaryIpAddress,
    int Port,
    IReadOnlyList<string> AllIpAddresses,
    string AccessibleLanUrl,
    bool IsLanReachable);

/// <summary>
/// Discovers the current machine's network identity (hostname, LAN IP addresses, port).
/// Pure synchronous — no I/O, results are derived from OS APIs.
/// </summary>
public interface INetworkInformationService
{
    /// <summary>Returns the current network information for the given API port.</summary>
    NetworkInformation GetCurrentNetworkInformation(int port);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="url"/> uses a raw IP address as the host
    /// (e.g. http://192.168.0.59:5000).
    /// Returns <see langword="false"/> for hostnames such as http://pixbridge.local.
    /// </summary>
    bool IsIpBasedUrl(string url);

    /// <summary>
    /// Replaces the host portion of <paramref name="existingUrl"/> with <paramref name="newIp"/>,
    /// preserving the scheme, port, and path.
    /// </summary>
    string ReplaceIpInUrl(string existingUrl, string newIp);
}
