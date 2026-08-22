using EventPhoto.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace EventPhoto.Infrastructure.Services.Licensing;

/// <summary>
/// Computes a deterministic SHA-256 hardware fingerprint from stable system identifiers.
///
/// <para>Inputs (Windows):</para>
/// <list type="bullet">
///   <item>Machine hostname</item>
///   <item>Machine SID (from the local security authority)</item>
///   <item>Primary disk serial number (Win32_DiskDrive)</item>
///   <item>CPU identifier (Win32_Processor)</item>
/// </list>
///
/// <para>Inputs (Linux/macOS fallback):</para>
/// <list type="bullet">
///   <item>Machine hostname</item>
///   <item>/etc/machine-id or /var/lib/dbus/machine-id</item>
/// </list>
///
/// <para>Failure contract: if any individual read fails, it is skipped.
/// If all reads fail, a fallback hash from the hostname alone is returned.
/// The method NEVER throws.</para>
/// </summary>
public sealed class FingerprintService(ILogger<FingerprintService> logger) : IFingerprintService
{
    public string ComputeHash()
    {
        try
        {
            var components = new List<string>();

            // Hostname — always available
            components.Add(NormalizeComponent(Environment.MachineName));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CollectWindowsComponents(components);
            }
            else
            {
                CollectUnixComponents(components);
            }

            // Remove any empty/null entries that failed to read
            var validComponents = components
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            if (validComponents.Count == 0)
            {
                // Ultimate fallback — hostname alone
                validComponents.Add(NormalizeComponent(Environment.MachineName));
                logger.LogWarning("FingerprintService: all hardware reads failed; using hostname-only fingerprint.");
            }

            var combined = string.Join("|", validComponents);
            return ComputeSha256(combined);
        }
        catch (Exception ex)
        {
            // Last-resort — never propagate
            logger.LogError(ex, "FingerprintService: unexpected error computing fingerprint; returning hostname fallback.");
            return ComputeSha256(Environment.MachineName.ToUpperInvariant());
        }
    }

    // ── Windows ──────────────────────────────────────────────────────────────

    private void CollectWindowsComponents(List<string> components)
    {
        // Machine SID
        try
        {
            var sid = GetWindowsMachineSid();
            if (!string.IsNullOrWhiteSpace(sid))
                components.Add(NormalizeComponent(sid));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "FingerprintService: could not read machine SID.");
        }

        // Disk serial via WMI
        try
        {
            var diskSerial = GetWindowsDiskSerial();
            if (!string.IsNullOrWhiteSpace(diskSerial))
                components.Add(NormalizeComponent(diskSerial));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "FingerprintService: could not read disk serial.");
        }

        // CPU identifier via WMI
        try
        {
            var cpuId = GetWindowsCpuId();
            if (!string.IsNullOrWhiteSpace(cpuId))
                components.Add(NormalizeComponent(cpuId));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "FingerprintService: could not read CPU identifier.");
        }
    }

    private static string? GetWindowsMachineSid()
    {
        // Use the built-in SecurityIdentifier API (no WMI needed)
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
        if (key?.GetValue("ProfilesDirectory") is null)
            return null;

        // Read machine SID from well-known registry location
        using var accountKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
            @"SECURITY\SAM\Domains\Account");
        var sidBytes = accountKey?.GetValue("V") as byte[];
        if (sidBytes is null || sidBytes.Length < 32)
            return null;

        // Extract the 3 RID sub-authorities (bytes 24-35) — unique per machine
        // This is the machine SID without the domain RID suffix
        return BitConverter.ToString(sidBytes, 24, Math.Min(12, sidBytes.Length - 24))
                           .Replace("-", "");
    }

    private static string? GetWindowsDiskSerial()
    {
        // Use P/Invoke-free approach via DriveInfo + registry
        // Fall back to environment-based serial if WMI unavailable
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT SerialNumber FROM Win32_DiskDrive WHERE MediaType='Fixed hard disk media' OR MediaType='Fixed Hard Disk'");
            using var results = searcher.Get();
            foreach (System.Management.ManagementObject obj in results)
            {
                var serial = obj["SerialNumber"]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(serial))
                    return serial;
            }
        }
        catch
        {
            // WMI not available — skip
        }

        return null;
    }

    private static string? GetWindowsCpuId()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT ProcessorId FROM Win32_Processor");
            using var results = searcher.Get();
            foreach (System.Management.ManagementObject obj in results)
            {
                var id = obj["ProcessorId"]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }
        }
        catch
        {
            // WMI not available — skip
        }

        return null;
    }

    // ── Unix / Linux ──────────────────────────────────────────────────────────

    private void CollectUnixComponents(List<string> components)
    {
        // /etc/machine-id (systemd) or /var/lib/dbus/machine-id
        var machineIdPaths = new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" };
        foreach (var path in machineIdPaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    var id = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        components.Add(NormalizeComponent(id));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "FingerprintService: could not read {Path}.", path);
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string NormalizeComponent(string value)
        => value.Trim().ToUpperInvariant();

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash  = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
