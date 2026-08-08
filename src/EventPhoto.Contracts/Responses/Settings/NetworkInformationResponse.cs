namespace EventPhoto.Contracts.Responses.Settings;

/// <summary>API response for the current machine network information.</summary>
public sealed record NetworkInformationResponse(
    string HostName,
    string MachineName,
    string PrimaryIpAddress,
    int Port,
    IReadOnlyList<string> AllIpAddresses,
    string AccessibleLanUrl,
    bool IsLanReachable);
