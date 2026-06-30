namespace EventPhoto.Contracts.Responses.Photos;

/// <summary>
/// Lightweight photo summary for gallery list views.
/// </summary>
/// <param name="Id">The photo identifier.</param>
/// <param name="FileName">The original file name.</param>
/// <param name="ThumbnailPath">The absolute path to the generated thumbnail.</param>
/// <param name="MimeType">The MIME type.</param>
/// <param name="CapturedAt">When the file was detected.</param>
/// <param name="ThumbnailStatus">The thumbnail generation status name.</param>
public sealed record PhotoSummaryResponse(
    Guid Id,
    string FileName,
    string ThumbnailPath,
    string MimeType,
    DateTimeOffset CapturedAt,
    string ThumbnailStatus);
