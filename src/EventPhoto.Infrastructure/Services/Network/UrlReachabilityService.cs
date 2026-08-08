using EventPhoto.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Infrastructure.Services.Network;

/// <summary>
/// HTTP-based implementation of <see cref="IUrlReachabilityService"/>.
/// Uses the named "UrlValidation" <see cref="System.Net.Http.HttpClient"/>.
/// </summary>
public sealed class UrlReachabilityService(
    IHttpClientFactory httpClientFactory,
    ILogger<UrlReachabilityService> logger)
    : IUrlReachabilityService
{
    private const int TimeoutSeconds = 8;

    /// <inheritdoc />
    public async Task<UrlReachabilityResult> TestAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("UrlValidation");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            sw.Stop();

            var isReachable = response.IsSuccessStatusCode || (int)response.StatusCode < 500;
            return new UrlReachabilityResult(isReachable, (int)response.StatusCode, sw.ElapsedMilliseconds, null);
        }
        catch (OperationCanceledException)
        {
            return new UrlReachabilityResult(false, null, null, $"Request timed out after {TimeoutSeconds} seconds.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogDebug(ex, "URL probe failed for {Url}", url);
            return new UrlReachabilityResult(false, null, null, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unexpected error probing {Url}", url);
            return new UrlReachabilityResult(false, null, null, ex.Message);
        }
    }
}
