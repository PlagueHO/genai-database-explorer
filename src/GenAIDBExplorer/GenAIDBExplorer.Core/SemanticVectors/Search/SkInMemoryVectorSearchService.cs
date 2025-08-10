using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.Extensions.VectorData;
using System.Reflection;

namespace GenAIDBExplorer.Core.SemanticVectors.Search;

/// <summary>
/// Vector search service powered by SK InMemoryVectorStore collections.
/// </summary>
public sealed class SkInMemoryVectorSearchService(InMemoryVectorStore store) : IVectorSearchService
{
    private readonly InMemoryVectorStore _store = store;
    private static async Task EnsureCollectionExistsAsync(object collection, CancellationToken cancellationToken)
    {
        var type = collection.GetType();
        var method = type.GetMethod("CreateCollectionIfNotExistsAsync", BindingFlags.Instance | BindingFlags.Public, new Type[] { });
        if (method is not null)
        {
            if (method.Invoke(collection, null) is Task t1) { await t1.ConfigureAwait(false); }
            return;
        }
        method = type.GetMethod("CreateCollectionIfNotExistsAsync", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(CancellationToken) });
        if (method is not null)
        {
            if (method.Invoke(collection, new object[] { cancellationToken }) is Task t2) { await t2.ConfigureAwait(false); }
            return;
        }
        method = type.GetMethod("CreateCollectionAsync", BindingFlags.Instance | BindingFlags.Public, new Type[] { });
        if (method is not null)
        {
            if (method.Invoke(collection, null) is Task t3) { await t3.ConfigureAwait(false); }
            return;
        }
        method = type.GetMethod("CreateCollectionAsync", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(CancellationToken) });
        if (method is not null)
        {
            if (method.Invoke(collection, new object[] { cancellationToken }) is Task t4) { await t4.ConfigureAwait(false); }
            return;
        }
    }

    public async Task<IEnumerable<(EntityVectorRecord Record, double Score)>> SearchAsync(ReadOnlyMemory<float> vector, int topK, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);
        if (vector.IsEmpty)
        {
            return Enumerable.Empty<(EntityVectorRecord, double)>();
        }

        var collection = _store.GetCollection<string, EntityVectorRecord>(infrastructure.CollectionName);
        await EnsureCollectionExistsAsync(collection!, cancellationToken).ConfigureAwait(false);

        var results = new List<(EntityVectorRecord, double)>();
        await foreach (var r in ((IVectorSearchable<EntityVectorRecord>)collection).SearchAsync(vector, topK, options: null, cancellationToken))
        {
            results.Add((r.Record, r.Score ?? 0d));
        }
        return results;
    }
}
