namespace EventPhoto.Contracts.Common;

/// <summary>Unified API response envelope.</summary>
/// <typeparam name="T">The payload type.</typeparam>
public sealed record ApiResponse<T>
{
    /// <summary>Gets a value indicating whether the request succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the response payload.</summary>
    public T? Data { get; init; }

    /// <summary>Gets the error message when the request failed.</summary>
    public string? Error { get; init; }

    /// <summary>Gets validation errors keyed by field name.</summary>
    public Dictionary<string, string[]>? ValidationErrors { get; init; }

    /// <summary>Creates a successful response.</summary>
    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };

    /// <summary>Creates a failed response.</summary>
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>Non-generic unified API response envelope.</summary>
public sealed record ApiResponse
{
    /// <summary>Gets a value indicating whether the request succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the error message when the request failed.</summary>
    public string? Error { get; init; }

    /// <summary>Gets validation errors keyed by field name.</summary>
    public Dictionary<string, string[]>? ValidationErrors { get; init; }

    /// <summary>Creates a successful response.</summary>
    public static ApiResponse Ok() => new() { Success = true };

    /// <summary>Creates a failed response.</summary>
    public static ApiResponse Fail(string error) => new() { Success = false, Error = error };
}
