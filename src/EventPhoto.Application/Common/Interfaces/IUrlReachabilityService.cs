namespace EventPhoto.Application.Common.Interfaces;

/// <summary>Tests whether a URL is reachable over HTTP/HTTPS from the current server.</summary>
public interface IUrlReachabilityService
{
    /// <summary>
    /// Probes <paramref name="url"/> with a GET request and returns reachability info.
    /// Never throws — errors are captured in the result.
    /// </summary>
    Task<UrlReachabilityResult> TestAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>Result of a URL reachability probe.</summary>
public sealed record UrlReachabilityResult(
    bool IsReachable,
    int? StatusCode,
    long? ResponseTimeMs,
    string? ErrorMessage);
