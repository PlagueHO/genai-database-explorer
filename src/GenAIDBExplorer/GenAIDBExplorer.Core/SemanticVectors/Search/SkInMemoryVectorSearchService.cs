using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.Extensions.VectorData;

namespace GenAIDBExplorer.Core.SemanticVectors.Search;

/// <summary>
/// Vector search service powered by SK InMemoryVectorStore collections.
/// </summary>
public sealed class SkInMemoryVectorSearchService(InMemoryVectorStore store) : IVectorSearchService
{
    private readonly InMemoryVectorStore _store = store;

    public async Task<IEnumerable<(EntityVectorRecord Record, double Score)>> SearchAsync(ReadOnlyMemory<float> vector, int topK, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);
        if (vector.IsEmpty)
        {
            return Enumerable.Empty<(EntityVectorRecord, double)>();
        }

    var collection = _store.GetCollection<string, EntityVectorRecord>(infrastructure.CollectionName);

        var results = new List<(EntityVectorRecord, double)>();
    await foreach (var r in ((IVectorSearchable<EntityVectorRecord>)collection).SearchAsync(vector, topK, options: null, cancellationToken))
        {
            results.Add((r.Record, r.Score ?? 0d));
        }
        return results;
    }
}
