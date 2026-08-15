using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for <see cref="GuestUploadSession"/> and <see cref="GuestUpload"/> aggregates.</summary>
public interface IGuestUploadRepository
{
    // ── Sessions ──────────────────────────────────────────────────────────────

    Task<GuestUploadSession?> GetSessionByIdAsync(Guid id, CancellationToken ct = default);
    Task<GuestUploadSession?> GetSessionByCodeAsync(string sessionCode, CancellationToken ct = default);
    Task<List<GuestUploadSession>> GetSessionsForEventAsync(Guid eventId, CancellationToken ct = default);
    Task AddSessionAsync(GuestUploadSession session, CancellationToken ct = default);

    // ── Uploads ───────────────────────────────────────────────────────────────

    Task<List<GuestUpload>> GetUploadsForEventAsync(Guid eventId, ModerationStatus? filter = null, CancellationToken ct = default);
    Task<GuestUpload?> GetUploadByIdAsync(Guid id, CancellationToken ct = default);
    Task AddUploadAsync(GuestUpload upload, CancellationToken ct = default);
    Task<int> CountPendingForEventAsync(Guid eventId, CancellationToken ct = default);
}
