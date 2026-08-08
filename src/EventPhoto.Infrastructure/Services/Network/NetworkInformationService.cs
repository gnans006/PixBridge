using EventPhoto.Application.Common.Interfaces;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace EventPhoto.Infrastructure.Services.Network;

/// <summary>
/// Discovers the current machine's LAN network identity using OS networking APIs.
/// Detects private IPv4 ranges: 192.168.x.x, 10.x.x.x, 172.16-31.x.x.
/// </summary>
public sealed class NetworkInformationService : INetworkInformationService
{
    /// <inheritdoc />
    public NetworkInformation GetCurrentNetworkInformation(int port)
    {
        var hostName = Dns.GetHostName();
        var machineName = Environment.MachineName;
        var allIps = DiscoverLanIpAddresses();
        var primaryIp = allIps.FirstOrDefault() ?? "127.0.0.1";
        var accessibleUrl = $"http://{primaryIp}:{port}";

        return new NetworkInformation(
            HostName: hostName,
            MachineName: machineName,
            PrimaryIpAddress: primaryIp,
            Port: port,
            AllIpAddresses: allIps,
            AccessibleLanUrl: accessibleUrl,
            IsLanReachable: allIps.Count > 0);
    }

    /// <inheritdoc />
    public bool IsIpBasedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return IPAddress.TryParse(uri.Host, out _);
    }

    /// <inheritdoc />
    public string ReplaceIpInUrl(string existingUrl, string newIp)
    {
        if (!Uri.TryCreate(existingUrl, UriKind.Absolute, out var uri))
        {
            return existingUrl;
        }

        var builder = new UriBuilder(uri)
        {
            Host = newIp,
        };
        return builder.Uri.ToString().TrimEnd('/');
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private static List<string> DiscoverLanIpAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Where(addr =>
                addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(addr.Address) &&
                !addr.Address.ToString().StartsWith("169.254") && // link-local
                IsPrivateIp(addr.Address))
            .Select(addr => addr.Address.ToString())
            .Distinct()
            .ToList();
    }

    private static bool IsPrivateIp(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,                                       // 10.0.0.0/8
            172 => bytes[1] is >= 16 and <= 31,              // 172.16.0.0/12
            192 => bytes[1] == 168,                          // 192.168.0.0/16
            _ => false,
        };
    }
}
