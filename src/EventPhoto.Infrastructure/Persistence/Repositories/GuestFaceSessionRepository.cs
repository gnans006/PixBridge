using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IGuestFaceSessionRepository"/>.
/// </summary>
public sealed class GuestFaceSessionRepository(AppDbContext context) : IGuestFaceSessionRepository
{
    /// <inheritdoc />
    public Task<GuestFaceSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.GuestFaceSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<GuestFaceSession?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => context.GuestFaceSessions.FirstOrDefaultAsync(s => s.SessionToken == token, cancellationToken);

    /// <inheritdoc />
    public Task<List<GuestFaceSession>> GetByStatusAsync(
        Guid eventId,
        FaceSessionStatus status,
        CancellationToken cancellationToken = default)
        => context.GuestFaceSessions
            .Where(s => s.EventId == eventId && s.Status == status)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<GuestFaceSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default)
        => context.GuestFaceSessions
            .Where(s => s.Status != FaceSessionStatus.Expired && s.ExpiresAt < DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(GuestFaceSession session, CancellationToken cancellationToken = default)
        => await context.GuestFaceSessions.AddAsync(session, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(GuestFaceSession session, CancellationToken cancellationToken = default)
    {
        context.GuestFaceSessions.Update(session);
        return Task.CompletedTask;
    }
}
