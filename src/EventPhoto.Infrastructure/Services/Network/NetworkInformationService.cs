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

    /// <summary>
    /// Virtual-adapter name fragments to deprioritise (Hyper-V, VMware, VirtualBox, etc.).
    /// These are moved to the end of the list so physical adapters are always preferred.
    /// </summary>
    private static readonly string[] VirtualAdapterKeywords =
    [
        "hyper-v", "vmware", "virtualbox", "vethernet", "tap adapter",
        "tap-windows", "microsoft wi-fi direct", "virtual", "loopback",
    ];

    private static List<string> DiscoverLanIpAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses
                .Where(addr =>
                    addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(addr.Address) &&
                    !addr.Address.ToString().StartsWith("169.254") &&
                    IsPrivateIp(addr.Address))
                .Select(addr => (Ip: addr.Address.ToString(), Nic: nic)))
            // Physical WiFi → Ethernet → other physical → virtual (last resort)
            .OrderBy(x => IsVirtualAdapter(x.Nic) ? 1 : 0)
            .ThenBy(x => x.Nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 0 :
                         x.Nic.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                                                     or NetworkInterfaceType.GigabitEthernet
                                                     or NetworkInterfaceType.FastEthernetT
                                                     or NetworkInterfaceType.FastEthernetFx ? 1 : 2)
            .Select(x => x.Ip)
            .Distinct()
            .ToList();
    }

    private static bool IsVirtualAdapter(NetworkInterface nic)
    {
        var desc = nic.Description.ToLowerInvariant();
        var name = nic.Name.ToLowerInvariant();
        return VirtualAdapterKeywords.Any(kw => desc.Contains(kw) || name.Contains(kw));
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
