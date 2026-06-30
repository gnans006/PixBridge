using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.ValueObjects;

/// <summary>
/// Value object representing a validated file system path.
/// </summary>
public sealed record FilePath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilePath"/> class.
    /// </summary>
    /// <param name="value">The normalized absolute file system path.</param>
    private FilePath(string value) => Value = value;

    /// <summary>
    /// Gets the absolute file system path.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a validated <see cref="FilePath"/> instance.
    /// </summary>
    /// <param name="path">The input path.</param>
    /// <returns>A validated file path.</returns>
    /// <exception cref="DomainException">Thrown when the path is invalid.</exception>
    public static FilePath Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new DomainException("File path cannot be empty.");
        }

        if (path.Contains("..", StringComparison.Ordinal))
        {
            throw new DomainException("File path contains invalid traversal sequences.");
        }

        var normalized = Path.GetFullPath(path);
        return new FilePath(normalized);
    }

    /// <summary>
    /// Returns the path string.
    /// </summary>
    /// <returns>The underlying path value.</returns>
    public override string ToString() => Value;

    /// <summary>
    /// Converts a <see cref="FilePath"/> to its string value.
    /// </summary>
    /// <param name="filePath">The file path to convert.</param>
    public static implicit operator string(FilePath filePath) => filePath.Value;
}
