namespace EventPhoto.Domain.Common;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error message, if any.</param>
    protected Result(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error ?? string.Empty;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed, or an empty string if it succeeded.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The failure message.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A successful typed result.</returns>
    public static Result<T> Success<T>(T value) => new(value, true);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="error">The failure message.</param>
    /// <returns>A failed typed result.</returns>
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the returned value.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, string? error = null)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value when the operation succeeded.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");
}
