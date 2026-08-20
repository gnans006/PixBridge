using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IUserRepository"/>.
/// </summary>
public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    /// <inheritdoc />
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => context.Users.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), cancellationToken);

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => context.Users.AnyAsync(u => u.Username == username.ToLowerInvariant(), cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsByUsernameAsync(string username, Guid? excludeId, CancellationToken cancellationToken = default)
        => context.Users.AnyAsync(
            u => u.Username == username.ToLowerInvariant() && (excludeId == null || u.Id != excludeId.Value),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken cancellationToken = default)
        => context.Users.AnyAsync(
            u => u.Email == email.ToLowerInvariant() && (excludeId == null || u.Id != excludeId.Value),
            cancellationToken);

    /// <inheritdoc />
    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => context.Users.OrderBy(u => u.FullName).ThenBy(u => u.Username).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountOwnerAccountsAsync(CancellationToken cancellationToken = default)
        => context.Users.CountAsync(
            u => u.IsActive && (u.Role == UserRole.StudioOwner || u.Role == UserRole.Admin),
            cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await context.Users.AddAsync(user, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
        => context.Users.CountAsync(u => u.IsActive, cancellationToken);
}
