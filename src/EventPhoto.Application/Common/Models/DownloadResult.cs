namespace EventPhoto.Application.Common.Models;

/// <summary>
/// Holds the data returned when a photo download is fulfilled.
/// </summary>
/// <param name="Data">The raw file bytes.</param>
/// <param name="MimeType">The MIME type of the file (e.g. <c>image/jpeg</c>).</param>
/// <param name="FileName">The original file name for the Content-Disposition header.</param>
public sealed record DownloadResult(byte[] Data, string MimeType, string FileName);
