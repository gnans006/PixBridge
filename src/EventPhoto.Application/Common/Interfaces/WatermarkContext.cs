namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Carries the runtime values used to resolve dynamic watermark tokens at download time.
/// </summary>
/// <param name="StudioName">
/// The studio name read from system settings (key <c>app.name</c>).
/// May be <see langword="null"/> when the setting is absent.
/// </param>
/// <param name="EventName">The display name of the event being downloaded from.</param>
/// <param name="EventDate">The date of the event.</param>
/// <param name="DownloadDate">The UTC timestamp of the download request.</param>
/// <param name="PhotoName">The original file name of the photo being downloaded.</param>
/// <param name="SessionId">
/// The guest face-search session token, if the download originated from a
/// face-search session. <see langword="null"/> for gallery downloads.
/// </param>
public sealed record WatermarkContext(
    string? StudioName,
    string EventName,
    DateOnly EventDate,
    DateTimeOffset DownloadDate,
    string PhotoName,
    string? SessionId);
