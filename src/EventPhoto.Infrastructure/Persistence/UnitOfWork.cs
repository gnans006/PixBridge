using EventPhoto.Domain.Interfaces;

namespace EventPhoto.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>.</summary>
public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
