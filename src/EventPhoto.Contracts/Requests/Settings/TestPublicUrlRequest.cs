namespace EventPhoto.Contracts.Requests.Settings;

/// <summary>Request body for testing whether a public base URL is reachable.</summary>
public sealed record TestPublicUrlRequest(string Url);
