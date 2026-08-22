using EventPhoto.Domain.Common;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Singleton record that represents the physical PixBridge installation.
/// Created once on first boot and never regenerated.
///
/// <para>
/// Separation from <see cref="Subscription"/>: subscription data is business data
/// (can be transferred, reset, cancelled). Installation identity is infrastructure
/// data — it survives subscription changes but tracks the physical machine.
/// </para>
/// </summary>
public sealed class InstallationRegistry : Entity
{
    /// <summary>Well-known fixed ID for the singleton installation record.</summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000003");

    private InstallationRegistry() { }

    /// <summary>
    /// Stable UUID generated on first boot. Never regenerated.
    /// Used to optionally bind a license key to one machine.
    /// </summary>
    public Guid InstallationId { get; private set; }

    /// <summary>
    /// SHA-256 hash of stable hardware identifiers (hostname, SID, disk serial, CPU).
    /// Only the hash is stored — raw hardware values are never persisted.
    /// </summary>
    public string MachineFingerprintHash { get; private set; } = string.Empty;

    /// <summary>UTC timestamp of first installation (first boot).</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>UTC timestamp of most recent startup validation.</summary>
    public DateTimeOffset LastValidatedAtUtc { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>Creates the installation registry record on first boot.</summary>
    public static InstallationRegistry Create(string machineFingerprintHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(machineFingerprintHash);

        var now = DateTimeOffset.UtcNow;
        return new InstallationRegistry
        {
            Id                    = SingletonId,
            InstallationId        = Guid.NewGuid(),
            MachineFingerprintHash = machineFingerprintHash,
            CreatedAtUtc          = now,
            LastValidatedAtUtc    = now,
            CreatedAt             = now,
            UpdatedAt             = now,
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the fingerprint hash when hardware changes are detected.
    /// Call only after writing a <c>MachineFingerprintChanged</c> audit entry.
    /// </summary>
    public void UpdateFingerprint(string newHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newHash);
        MachineFingerprintHash = newHash;
        LastValidatedAtUtc     = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>Records a successful startup validation pass.</summary>
    public void RecordValidation()
    {
        LastValidatedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }
}
