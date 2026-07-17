using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for <see cref="GuestFaceSession"/> persistence.</summary>
public interface IGuestFaceSessionRepository
{
    /// <summary>Returns a session by its primary key.</summary>
    Task<GuestFaceSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a session by its opaque session token.</summary>
    Task<GuestFaceSession?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Returns sessions with a specific status for a given event.</summary>
    Task<List<GuestFaceSession>> GetByStatusAsync(
        Guid eventId,
        FaceSessionStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all sessions that have passed their <c>ExpiresAt</c> timestamp but are not yet marked Expired.</summary>
    Task<List<GuestFaceSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new session.</summary>
    Task AddAsync(GuestFaceSession session, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing session.</summary>
    Task UpdateAsync(GuestFaceSession session, CancellationToken cancellationToken = default);
}
