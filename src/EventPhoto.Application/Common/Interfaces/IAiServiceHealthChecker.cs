namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Performs a health probe against the Python face-recognition service.
/// Implemented in the Infrastructure layer to keep HttpClient concerns out of Application.
/// </summary>
public interface IAiServiceHealthChecker
{
    /// <summary>
    /// Sends a GET /health request to the face-recognition service.
    /// </summary>
    /// <returns>
    /// <c>(isHealthy: true, detail: null)</c> when the service responds 2xx within the timeout;
    /// <c>(isHealthy: false, detail: &lt;reason&gt;)</c> when the service is unreachable or returns an error.
    /// </returns>
    Task<(bool IsHealthy, long ElapsedMs, string? Detail)> CheckAsync(CancellationToken cancellationToken = default);
}
