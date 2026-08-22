namespace EventPhoto.Application.Common.Models;

/// <summary>
/// Holds the data returned when a photo download is fulfilled.
///
/// <para>
/// When <see cref="FilePath"/> is set, the controller should stream the file
/// directly from disk using <c>PhysicalFile</c> — avoiding loading the full
/// image into RAM. <see cref="Data"/> will be <see langword="null"/> in that case.
/// </para>
/// <para>
/// When <see cref="FilePath"/> is <see langword="null"/>, the controller falls
/// back to returning <see cref="Data"/> bytes (legacy path, e.g. watermark
/// processing failed before cache could be written).
/// </para>
/// </summary>
/// <param name="FilePath">Absolute path to a cached or original file on disk. Preferred path.</param>
/// <param name="Data">Raw file bytes. Used only when <see cref="FilePath"/> is null.</param>
/// <param name="MimeType">MIME type (e.g. <c>image/jpeg</c>).</param>
/// <param name="FileName">Original file name for the Content-Disposition header.</param>
public sealed record DownloadResult(
    string? FilePath,
    byte[]? Data,
    string MimeType,
    string FileName)
{
    /// <summary>Creates a result that streams directly from a file on disk (zero RAM).</summary>
    public static DownloadResult FromPath(string filePath, string mimeType, string fileName)
        => new(filePath, null, mimeType, fileName);

    /// <summary>Creates a result backed by in-memory bytes (fallback path).</summary>
    public static DownloadResult FromBytes(byte[] data, string mimeType, string fileName)
        => new(null, data, mimeType, fileName);
}
