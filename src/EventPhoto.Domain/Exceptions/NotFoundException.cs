namespace EventPhoto.Domain.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found.
/// </summary>
public class NotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="entityName">The missing entity name.</param>
    /// <param name="key">The lookup key.</param>
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }
}
