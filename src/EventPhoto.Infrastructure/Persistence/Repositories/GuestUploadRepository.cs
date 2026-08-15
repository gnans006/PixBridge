using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>PostgreSQL-backed implementation of <see cref="IGuestUploadRepository"/>.</summary>
public sealed class GuestUploadRepository(AppDbContext context) : IGuestUploadRepository
{
    public Task<GuestUploadSession?> GetSessionByIdAsync(Guid id, CancellationToken ct = default)
        => context.GuestUploadSessions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<GuestUploadSession?> GetSessionByCodeAsync(string sessionCode, CancellationToken ct = default)
        => context.GuestUploadSessions
            .FirstOrDefaultAsync(s => s.SessionCode == sessionCode.ToUpperInvariant(), ct);

    public Task<List<GuestUploadSession>> GetSessionsForEventAsync(Guid eventId, CancellationToken ct = default)
        => context.GuestUploadSessions
            .Where(s => s.EventId == eventId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task AddSessionAsync(GuestUploadSession session, CancellationToken ct = default)
        => await context.GuestUploadSessions.AddAsync(session, ct);

    public Task<List<GuestUpload>> GetUploadsForEventAsync(
        Guid eventId,
        ModerationStatus? filter = null,
        CancellationToken ct = default)
    {
        var query = context.GuestUploads.Where(u => u.EventId == eventId);
        if (filter.HasValue)
            query = query.Where(u => u.ModerationStatus == filter.Value);
        return query.OrderByDescending(u => u.UploadedAt).ToListAsync(ct);
    }

    public Task<GuestUpload?> GetUploadByIdAsync(Guid id, CancellationToken ct = default)
        => context.GuestUploads.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddUploadAsync(GuestUpload upload, CancellationToken ct = default)
        => await context.GuestUploads.AddAsync(upload, ct);

    public Task<int> CountPendingForEventAsync(Guid eventId, CancellationToken ct = default)
        => context.GuestUploads
            .CountAsync(u => u.EventId == eventId && u.ModerationStatus == ModerationStatus.Pending, ct);
}
