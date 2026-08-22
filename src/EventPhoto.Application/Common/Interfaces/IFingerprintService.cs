namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Computes a deterministic hardware fingerprint for tamper detection.
///
/// <para>Design constraints:</para>
/// <list type="bullet">
///   <item>Returns ONLY a SHA-256 hex hash — raw hardware values are never exposed or stored.</item>
///   <item>Must never throw — returns a deterministic fallback hash if hardware reads fail.</item>
///   <item>Must be deterministic: same machine = same hash across restarts.</item>
///   <item>Must survive minor hardware changes (NIC swap) without false positives.</item>
/// </list>
/// </summary>
public interface IFingerprintService
{
    /// <summary>
    /// Computes and returns a SHA-256 hex hash of stable hardware identifiers.
    /// Never throws. Returns a hostname-only fallback hash if all hardware reads fail.
    /// </summary>
    string ComputeHash();
}
