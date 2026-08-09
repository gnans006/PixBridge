using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for <see cref="FaceCluster"/> persistence.</summary>
public interface IFaceClusterRepository
{
    /// <summary>Adds a new cluster.</summary>
    Task AddAsync(FaceCluster cluster, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing cluster (centroid + photo count after re-clustering).</summary>
    Task UpdateAsync(FaceCluster cluster, CancellationToken cancellationToken = default);

    /// <summary>Returns all clusters for a given event.</summary>
    Task<List<FaceCluster>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Returns the total cluster count for a given event.</summary>
    Task<int> CountByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
}
