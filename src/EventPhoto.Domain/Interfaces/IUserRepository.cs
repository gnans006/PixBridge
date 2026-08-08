using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>
/// Repository contract for <see cref="User"/> entities.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching user, or <see langword="null"/> if not found.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching user, or <see langword="null"/> if not found.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching user, or <see langword="null"/> if not found.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a username already exists.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the username exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user.
    /// </summary>
    /// <param name="user">The user to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all studio users (active and inactive).
    /// </summary>
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a username already exists, optionally excluding a user by identifier.
    /// </summary>
    Task<bool> ExistsByUsernameAsync(string username, Guid? excludeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an email already exists, optionally excluding a user by identifier.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the count of active StudioOwner (or Admin) accounts.
    /// </summary>
    Task<int> CountOwnerAccountsAsync(CancellationToken cancellationToken = default);
}
