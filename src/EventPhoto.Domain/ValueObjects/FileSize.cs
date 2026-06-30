using EventPhoto.Domain.Exceptions;

namespace EventPhoto.Domain.ValueObjects;

/// <summary>
/// Value object representing a file size in bytes.
/// </summary>
public sealed record FileSize
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSize"/> class.
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    private FileSize(long bytes) => Bytes = bytes;

    /// <summary>
    /// Gets the size in bytes.
    /// </summary>
    public long Bytes { get; }

    /// <summary>
    /// Creates a <see cref="FileSize"/> value object.
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>A validated file size.</returns>
    /// <exception cref="DomainException">Thrown when the size is negative.</exception>
    public static FileSize Create(long bytes)
    {
        if (bytes < 0)
        {
            throw new DomainException("File size cannot be negative.");
        }

        return new FileSize(bytes);
    }

    /// <summary>
    /// Converts the size to a human-readable string.
    /// </summary>
    /// <returns>A size string formatted in B, KB, MB, or GB.</returns>
    public string ToHumanReadable() => Bytes switch
    {
        < 1024 => $"{Bytes} B",
        < 1_048_576 => $"{Bytes / 1024.0:F1} KB",
        < 1_073_741_824 => $"{Bytes / 1_048_576.0:F1} MB",
        _ => $"{Bytes / 1_073_741_824.0:F2} GB"
    };

    /// <summary>
    /// Converts a <see cref="FileSize"/> to its byte value.
    /// </summary>
    /// <param name="fileSize">The file size to convert.</param>
    public static implicit operator long(FileSize fileSize) => fileSize.Bytes;
}
