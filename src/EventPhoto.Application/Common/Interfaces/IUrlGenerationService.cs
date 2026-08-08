namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Generates canonical URLs for all PixBridge resources using
/// <c>ApplicationSettings.PublicBaseUrl</c> as the base.
///
/// NEVER use HttpContext.Request.Host, machine IP, or localhost in generated URLs.
/// Always go through this service.
/// </summary>
public interface IUrlGenerationService
{
    /// <summary>Returns the gallery URL guests open to browse photos (also encoded in QR codes).</summary>
    Task<string> GenerateGalleryUrlAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Returns the admin event-workspace URL.</summary>
    Task<string> GenerateEventUrlAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Returns the direct photo download URL.</summary>
    Task<string> GenerateDownloadUrlAsync(Guid photoId, CancellationToken cancellationToken = default);

    /// <summary>Returns the face-search landing URL for guests.</summary>
    Task<string> GenerateFaceSearchUrlAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the URL encoded inside a QR code (identical to <see cref="GenerateGalleryUrlAsync"/>).
    /// Provided as a named method to make the intent explicit at call sites.
    /// </summary>
    Task<string> GenerateQrUrlAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Returns the raw PublicBaseUrl with no trailing slash.</summary>
    Task<string> GetPublicBaseUrlAsync(CancellationToken cancellationToken = default);
}
