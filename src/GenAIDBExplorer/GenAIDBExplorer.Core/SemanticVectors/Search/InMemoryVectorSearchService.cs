using System.Collections.Concurrent;
using System.Linq;
using System.Numerics.Tensors;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using GenAIDBExplorer.Core.SemanticVectors.Indexing;

namespace GenAIDBExplorer.Core.SemanticVectors.Search;

public class InMemoryVectorSearchService : IVectorSearchService
{
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
