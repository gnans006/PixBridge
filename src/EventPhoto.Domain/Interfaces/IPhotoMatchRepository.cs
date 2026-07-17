using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for <see cref="PhotoMatch"/> persistence.</summary>
public interface IPhotoMatchRepository
{
    /// <summary>Returns all matches for a given session, ordered by similarity score descending.</summary>
    Task<List<PhotoMatch>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of matches for a session, ordered by similarity descending.
    /// </summary>
    Task<List<PhotoMatch>> GetPagedBySessionIdAsync(
        Guid sessionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the total number of matches for a session.</summary>
    Task<int> CountBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a specific photo is in the matched set of a session.
    /// Used to validate download authorisation when <c>RestrictDownloadsToMatchedPhotos</c> is enabled.
    /// </summary>
    Task<bool> IsPhotoMatchedInSessionAsync(
        Guid sessionId,
        Guid photoId,
        CancellationToken cancellationToken = default);

    /// <summary>Bulk-inserts all matches for a completed search session.</summary>
    Task AddRangeAsync(IEnumerable<PhotoMatch> matches, CancellationToken cancellationToken = default);
}
