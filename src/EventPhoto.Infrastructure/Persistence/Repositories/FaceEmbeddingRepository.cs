using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using EventPhoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace EventPhoto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL + pgvector implementation of <see cref="IFaceEmbeddingRepository"/>.
/// Vector search uses the HNSW cosine-similarity index for O(log N) performance.
/// </summary>
public sealed class FaceEmbeddingRepository(AppDbContext context) : IFaceEmbeddingRepository
{
    /// <inheritdoc />
    public async Task AddAsync(FaceEmbedding embedding, CancellationToken cancellationToken = default)
        => await context.FaceEmbeddings.AddAsync(embedding, cancellationToken);

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<FaceEmbedding> embeddings, CancellationToken cancellationToken = default)
        => await context.FaceEmbeddings.AddRangeAsync(embeddings, cancellationToken);

    /// <inheritdoc />
    public Task<List<FaceEmbedding>> GetByPhotoIdAsync(Guid photoId, CancellationToken cancellationToken = default)
        => context.FaceEmbeddings
            .Where(f => f.PhotoId == photoId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task DeleteByPhotoIdAsync(Guid photoId, CancellationToken cancellationToken = default)
    {
        var embeddings = await context.FaceEmbeddings
            .Where(f => f.PhotoId == photoId)
            .ToListAsync(cancellationToken);
        context.FaceEmbeddings.RemoveRange(embeddings);
    }

    /// <inheritdoc />
    public async Task<List<(Guid PhotoId, float Similarity)>> SearchByEmbeddingAsync(
        Guid eventId,
        float[] queryEmbedding,
        float threshold,
        int topK = 200,
        CancellationToken cancellationToken = default)
    {
        // Pgvector.EntityFrameworkCore CosineDistance() maps to the <=> operator.
        // Cosine similarity = 1 - cosine_distance.
        // The HNSW index on face_embeddings.embedding is used automatically.
        var queryVector = new Vector(queryEmbedding);

        var results = await context.FaceEmbeddings
            .Where(f => f.EventId == eventId)
            .OrderBy(f => f.Embedding.CosineDistance(queryVector))
            .Take(topK)
            .Select(f => new
            {
                f.PhotoId,
                Distance = f.Embedding.CosineDistance(queryVector)
            })
            .ToListAsync(cancellationToken);

        return results
            .Select(r => (r.PhotoId, Similarity: 1f - (float)r.Distance))
            .Where(r => r.Similarity >= threshold)
            .ToList();
    }

    /// <inheritdoc />
    public Task<int> CountByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        => context.FaceEmbeddings.CountAsync(f => f.EventId == eventId, cancellationToken);
}
