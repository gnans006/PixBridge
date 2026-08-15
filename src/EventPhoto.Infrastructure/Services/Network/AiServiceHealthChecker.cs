using EventPhoto.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Infrastructure.Services.Network;

/// <summary>
/// Probes the Python face-recognition service via the pre-configured
/// <c>FaceRecognitionHealth</c> named HttpClient (4-second timeout, no Polly).
/// </summary>
internal sealed class AiServiceHealthChecker(
    IHttpClientFactory httpClientFactory,
    ILogger<AiServiceHealthChecker> logger)
    : IAiServiceHealthChecker
{
    public async Task<(bool IsHealthy, long ElapsedMs, string? Detail)> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var client = httpClientFactory.CreateClient("FaceRecognitionHealth");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            var response = await client.GetAsync("/health", cts.Token);
            sw.Stop();

            if (response.IsSuccessStatusCode)
                return (true, sw.ElapsedMilliseconds, null);

            return (false, sw.ElapsedMilliseconds, $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogDebug(ex, "AI service health probe failed");
            return (false, sw.ElapsedMilliseconds, "Service unreachable — ensure the face recognition service is running.");
        }
    }
}
