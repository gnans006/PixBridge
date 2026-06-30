namespace EventPhoto.Domain.Exceptions;

/// <summary>
/// Base exception for domain rule violations.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
