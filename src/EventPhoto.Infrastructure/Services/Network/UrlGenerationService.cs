using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Interfaces;

namespace EventPhoto.Infrastructure.Services.Network;

/// <summary>
/// Generates canonical URLs for all PixBridge resources.
/// Reads <c>PublicBaseUrl</c> from <see cref="IApplicationSettingsRepository"/> — never from
/// HttpContext, machine IP, or hardcoded strings.
/// </summary>
public sealed class UrlGenerationService(IApplicationSettingsRepository settingsRepository)
    : IUrlGenerationService
{
    /// <inheritdoc />
    public async Task<string> GetPublicBaseUrlAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsRepository.GetOrCreateDefaultAsync(cancellationToken);
        return settings.PublicBaseUrl.TrimEnd('/');
    }

    /// <inheritdoc />
    public async Task<string> GenerateGalleryUrlAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = await GetPublicBaseUrlAsync(cancellationToken);
        return $"{baseUrl}/gallery/{eventId}";
    }

    /// <inheritdoc />
    public async Task<string> GenerateEventUrlAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = await GetPublicBaseUrlAsync(cancellationToken);
        return $"{baseUrl}/admin/events/{eventId}";
    }

    /// <inheritdoc />
    public async Task<string> GenerateDownloadUrlAsync(
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = await GetPublicBaseUrlAsync(cancellationToken);
        return $"{baseUrl}/api/photos/{photoId}/download";
    }

    /// <inheritdoc />
    public async Task<string> GenerateFaceSearchUrlAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = await GetPublicBaseUrlAsync(cancellationToken);
        return $"{baseUrl}/gallery/{eventId}/find";
    }

    /// <inheritdoc />
    public async Task<string> GenerateQrUrlAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
        => await GenerateGalleryUrlAsync(eventId, cancellationToken);
}
