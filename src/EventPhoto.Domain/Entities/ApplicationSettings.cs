using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.Entities;

/// <summary>
/// Central application configuration aggregate (single-row pattern).
/// Only one active record ever exists. <see cref="SingletonId"/> is always used as the primary key.
/// </summary>
public sealed class ApplicationSettings : AggregateRoot
{
    /// <summary>Well-known fixed ID for the singleton application-settings record.</summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    private ApplicationSettings()
    {
    }

    /// <summary>Gets the display name of the photography studio.</summary>
    public string StudioName { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable server/machine name shown in the UI.</summary>
    public string ServerName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the canonical public base URL used for all generated links and QR codes.
    /// Must start with http:// or https://. Never ends with a trailing slash.
    /// </summary>
    public string PublicBaseUrl { get; private set; } = string.Empty;

    /// <summary>Gets the TCP port on which the API server listens.</summary>
    public int ServerPort { get; private set; }

    /// <summary>Gets the default gallery mode applied to newly created events.</summary>
    public GalleryMode DefaultEventGalleryMode { get; private set; }

    /// <summary>Gets whether watermarking should be enabled by default on new events.</summary>
    public bool EnableWatermarkByDefault { get; private set; }

    /// <summary>Gets whether face recognition should be enabled by default on new events.</summary>
    public bool EnableFaceRecognitionByDefault { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the default settings record. Uses <see cref="SingletonId"/> as the primary key
    /// and seeds sensible defaults so the application starts without manual configuration.
    /// </summary>
    public static ApplicationSettings CreateDefault()
    {
        return new ApplicationSettings
        {
            Id = SingletonId,
            StudioName = "My Photography Studio",
            ServerName = Environment.MachineName,
            PublicBaseUrl = "http://localhost:5000",
            ServerPort = 5000,
            DefaultEventGalleryMode = GalleryMode.GalleryOnly,
            EnableWatermarkByDefault = false,
            EnableFaceRecognitionByDefault = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Updates all configurable settings.
    /// Domain invariants are validated in the entity to guarantee consistency regardless of caller.
    /// </summary>
    public void Update(
        string studioName,
        string serverName,
        string publicBaseUrl,
        int serverPort,
        GalleryMode defaultEventGalleryMode,
        bool enableWatermarkByDefault,
        bool enableFaceRecognitionByDefault)
    {
        if (string.IsNullOrWhiteSpace(studioName))
            throw new DomainException("Studio name is required.");
        if (studioName.Length > 200)
            throw new DomainException("Studio name must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(serverName))
            throw new DomainException("Server name is required.");
        if (serverName.Length > 100)
            throw new DomainException("Server name must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            throw new DomainException("Public base URL is required.");
        if (!Uri.TryCreate(publicBaseUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("Public base URL must be a valid http:// or https:// URL.");

        if (serverPort is < 1 or > 65535)
            throw new DomainException("Server port must be between 1 and 65535.");

        StudioName = studioName.Trim();
        ServerName = serverName.Trim();
        PublicBaseUrl = publicBaseUrl.Trim().TrimEnd('/');
        ServerPort = serverPort;
        DefaultEventGalleryMode = defaultEventGalleryMode;
        EnableWatermarkByDefault = enableWatermarkByDefault;
        EnableFaceRecognitionByDefault = enableFaceRecognitionByDefault;
        Touch();
    }

    /// <summary>
    /// Updates only the PublicBaseUrl. Used by startup IP auto-detection without changing other fields.
    /// </summary>
    public void UpdatePublicBaseUrl(string publicBaseUrl)
    {
        if (!Uri.TryCreate(publicBaseUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("Public base URL must be a valid http:// or https:// URL.");

        PublicBaseUrl = publicBaseUrl.Trim().TrimEnd('/');
        Touch();
    }
}
