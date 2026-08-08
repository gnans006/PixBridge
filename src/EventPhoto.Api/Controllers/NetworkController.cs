using EventPhoto.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Exposes network topology information for LAN diagnostics and guest access configuration.
/// </summary>
[ApiController]
[Route("api/system")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class NetworkController : ControllerBase
{
    private readonly ISystemSettingRepository _settings;
    private readonly IConfiguration _config;

    /// <summary>Initializes a new instance of <see cref="NetworkController"/>.</summary>
    public NetworkController(ISystemSettingRepository settings, IConfiguration config)
    {
        _settings = settings;
        _config = config;
    }

    /// <summary>
    /// Returns the server's current network topology: IPs, port, hostname, and public base URL.
    /// Useful for LAN diagnostics, QR troubleshooting, and guest-access validation.
    /// </summary>
    [HttpGet("network")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNetworkInfo(CancellationToken cancellationToken)
    {
        var hostname = Dns.GetHostName();

        // Collect all active LAN IPv4 addresses (skip loopback and link-local)
        var lanAddresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                     && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                     && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork
                     && !IPAddress.IsLoopback(a.Address)
                     && !a.Address.ToString().StartsWith("169.254"))
            .Select(a => a.Address.ToString())
            .ToList();

        var primaryIp = lanAddresses.FirstOrDefault() ?? "127.0.0.1";

        // Read port from Kestrel configuration (fallback to 5000)
        var kestrelUrl = _config["Kestrel:Endpoints:Http:Url"] ?? $"http://0.0.0.0:5000";
        var port = Uri.TryCreate(kestrelUrl.Replace("0.0.0.0", "127.0.0.1"), UriKind.Absolute, out var kestrelUri)
            ? kestrelUri.Port
            : 5000;

        // Read publicBaseUrl from the persisted system setting
        var serverUrlSetting = await _settings.GetByKeyAsync("app.serverUrl", cancellationToken);
        var publicBaseUrl = serverUrlSetting?.Value ?? $"http://{primaryIp}:{port}";

        return Ok(new
        {
            hostname,
            primaryIp,
            allIpAddresses = lanAddresses,
            port,
            accessibleUrl = $"http://{primaryIp}:{port}",
            publicBaseUrl,
            qrBaseUrl   = publicBaseUrl,
            serverTime  = DateTimeOffset.UtcNow,
        });
    }
}
