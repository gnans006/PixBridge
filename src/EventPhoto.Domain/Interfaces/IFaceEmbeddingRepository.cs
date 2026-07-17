using EventPhoto.Domain.Entities;

namespace EventPhoto.Domain.Interfaces;

/// <summary>Repository contract for <see cref="FaceEmbedding"/> persistence.</summary>
public interface IFaceEmbeddingRepository
{
    /// <summary>Adds a new face embedding.</summary>
    Task AddAsync(FaceEmbedding embedding, CancellationToken cancellationToken = default);

    /// <summary>Adds a batch of face embeddings in a single operation.</summary>
    Task AddRangeAsync(IEnumerable<FaceEmbedding> embeddings, CancellationToken cancellationToken = default);

    /// <summary>Returns all embeddings for a given photo.</summary>
    Task<List<FaceEmbedding>> GetByPhotoIdAsync(Guid photoId, CancellationToken cancellationToken = default);

    /// <summary>Deletes all face embeddings for the specified photo.</summary>
    Task DeleteByPhotoIdAsync(Guid photoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a pgvector cosine-similarity nearest-neighbour search against the HNSW index.
    /// Returns the top <paramref name="topK"/> (PhotoId, similarity) pairs where
    /// similarity &gt;= <paramref name="threshold"/> within the given event.
    /// </summary>
    Task<List<(Guid PhotoId, float Similarity)>> SearchByEmbeddingAsync(
        Guid eventId,
        float[] queryEmbedding,
        float threshold,
        int topK = 200,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the total number of face embeddings stored for an event.</summary>
    Task<int> CountByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
}
