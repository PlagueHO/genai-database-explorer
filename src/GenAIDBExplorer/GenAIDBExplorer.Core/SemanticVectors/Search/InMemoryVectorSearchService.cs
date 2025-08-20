using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using GenAIDBExplorer.Core.SemanticVectors.Indexing;

namespace GenAIDBExplorer.Core.SemanticVectors.Search;

/// <summary>
/// Provides an in-memory vector search service that performs similarity searches using cosine similarity.
/// This implementation uses a simple in-memory collection and tensor operations for vector comparisons,
/// making it suitable for development, testing, and small-scale deployments.
/// </summary>
public class InMemoryVectorSearchService : IVectorSearchService
{
    /// <summary>
    /// Performs a vector similarity search using cosine similarity to find the most relevant entity vector records.
    /// This method retrieves all vectors from the specified collection, calculates cosine similarity scores
    /// against the query vector, and returns the top K results ordered by similarity score.
    /// </summary>
    /// <param name="vector">The query vector to search with. Must not be empty.</param>
    /// <param name="topK">The maximum number of results to return. Must be greater than zero.</param>
    /// <param name="infrastructure">The vector infrastructure configuration containing collection details.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous search operation. The result contains an enumerable of tuples,
    /// where each tuple contains an EntityVectorRecord and its cosine similarity score (ranging from -1 to 1,
    /// where 1 indicates perfect similarity).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="topK"/> is zero or negative.</exception>
    /// <remarks>
    /// This implementation performs an exhaustive search over all vectors in the collection, making it suitable
    /// for small to medium-sized collections but not optimal for large-scale vector searches. The cosine similarity
    /// is calculated using tensor primitives for efficient computation.
    /// </remarks>
    public Task<IEnumerable<(EntityVectorRecord Record, double Score)>> SearchAsync(ReadOnlyMemory<float> vector, int topK, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);
        if (vector.IsEmpty) return Task.FromResult(Enumerable.Empty<(EntityVectorRecord, double)>());

        var collection = InMemoryVectorIndexWriter.GetCollection(infrastructure.CollectionName).Values;
        var results = collection
            .Select(r => (Record: r, Score: (double)TensorPrimitives.CosineSimilarity(vector.Span, r.Vector.Span)))
            .OrderByDescending(x => x.Score)
            .Take(topK);

        return Task.FromResult(results);
    }
}
