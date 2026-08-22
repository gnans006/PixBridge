using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventPhoto.Infrastructure.Services.Network;

/// <summary>
/// Generates canonical URLs for all PixBridge resources.
/// Reads <c>PublicBaseUrl</c> from <see cref="IApplicationSettingsRepository"/> and caches
/// it in-memory for <see cref="CacheTtlSeconds"/> seconds to avoid a DB hit on every call.
/// Registered as a singleton — uses <see cref="IServiceScopeFactory"/> to resolve scoped
/// dependencies safely.
/// </summary>
public sealed class UrlGenerationService(IServiceScopeFactory scopeFactory)
    : IUrlGenerationService
{
    private const int CacheTtlSeconds = 60;

    private string? _cachedBaseUrl;
    private DateTimeOffset _cacheExpiresAt = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    /// <inheritdoc />
    public async Task<string> GetPublicBaseUrlAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        if (_cachedBaseUrl is not null && now < _cacheExpiresAt)
            return _cachedBaseUrl;

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedBaseUrl is not null && DateTimeOffset.UtcNow < _cacheExpiresAt)
                return _cachedBaseUrl;

            using var scope = scopeFactory.CreateScope();
            var settingsRepository = scope.ServiceProvider.GetRequiredService<IApplicationSettingsRepository>();
            var settings = await settingsRepository.GetOrCreateDefaultAsync(cancellationToken);
            _cachedBaseUrl = settings.PublicBaseUrl.TrimEnd('/');
            _cacheExpiresAt = DateTimeOffset.UtcNow.AddSeconds(CacheTtlSeconds);
            return _cachedBaseUrl;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>Invalidates the in-memory cache. Call after updating PublicBaseUrl in settings.</summary>
    public void InvalidateCache() => _cacheExpiresAt = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public async Task<string> GenerateGalleryUrlAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var baseUrl = await GetPublicBaseUrlAsync(cancellationToken);
        return $"{baseUrl}/gallery/{eventId}";
    }

    /// <inheritdoc />
    public async Task<string> GenerateEventUrlAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var baseUrl = await GetPublicBaseUrlAsync(cancellationToken);
        return $"{baseUrl}/admin/events/{eventId}";
    }

    /// <inheritdoc />
    public async Task<string> GenerateDownloadUrlAsync(Guid photoId, CancellationToken cancellationToken = default)
    {
        var baseUrl = await GetPublicBaseUrlAsync(cancellationToken);
        return $"{baseUrl}/api/photos/{photoId}/download";
    }

    /// <inheritdoc />
    public async Task<string> GenerateFaceSearchUrlAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var baseUrl = await GetPublicBaseUrlAsync(cancellationToken);
        return $"{baseUrl}/gallery/{eventId}/find";
    }

    /// <inheritdoc />
    public async Task<string> GenerateQrUrlAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await GenerateGalleryUrlAsync(eventId, cancellationToken);
}