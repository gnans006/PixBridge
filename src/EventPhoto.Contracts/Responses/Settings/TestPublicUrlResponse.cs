namespace EventPhoto.Contracts.Responses.Settings;

/// <summary>API response for a public URL connectivity test.</summary>
public sealed record TestPublicUrlResponse(
    bool IsReachable,
    int? StatusCode,
    long? ResponseTimeMs,
    string? ErrorMessage);
