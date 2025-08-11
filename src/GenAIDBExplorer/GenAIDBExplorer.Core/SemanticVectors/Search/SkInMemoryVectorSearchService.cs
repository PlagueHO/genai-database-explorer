using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.Extensions.VectorData;
using System.Reflection;
using GenAIDBExplorer.Core.Repository.Performance;

namespace GenAIDBExplorer.Core.SemanticVectors.Search;

/// <summary>
/// Vector search service powered by SK InMemoryVectorStore collections.
/// </summary>
public sealed class SkInMemoryVectorSearchService(InMemoryVectorStore store, IPerformanceMonitor performanceMonitor) : IVectorSearchService
{
    private readonly InMemoryVectorStore _store = store;
    private readonly IPerformanceMonitor _performanceMonitor = performanceMonitor;
    private static Task EnsureCollectionExistsAsync(VectorStoreCollection<string, EntityVectorRecord> collection, CancellationToken cancellationToken)
        => collection.EnsureCollectionExistsAsync(cancellationToken);

    private static async Task EnsureCollectionExistsOnStoreAsync(InMemoryVectorStore store, string collectionName, CancellationToken cancellationToken)
    {
        var type = store.GetType();
        foreach (var methodName in new[] { "CreateCollectionIfNotExistsAsync", "CreateCollectionAsync", "CreateCollection" })
        {
            foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(x => x.Name == methodName))
            {
                try
                {
                    MethodInfo toInvoke = m;
                    if (m.IsGenericMethodDefinition)
                    {
                        try { toInvoke = m.MakeGenericMethod(typeof(EntityVectorRecord)); }
                        catch { toInvoke = m; }
                    }

                    var parms = toInvoke.GetParameters();
                    object? result = null;
                    if (parms.Length == 2 && parms[0].ParameterType == typeof(string) && parms[1].ParameterType == typeof(CancellationToken))
                    {
                        result = toInvoke.Invoke(store, new object[] { collectionName, cancellationToken });
                    }
                    else if (parms.Length == 1 && parms[0].ParameterType == typeof(string))
                    {
                        result = toInvoke.Invoke(store, new object[] { collectionName });
                    }
                    else
                    {
                        continue;
                    }

                    if (result is Task t) { await t.ConfigureAwait(false); }
                    return;
                }
                catch { }
            }
        }
    }

    private static async Task EnsureCollectionExistsOnCollectionAsync(object collection, CancellationToken cancellationToken)
    {
        var type = collection.GetType();
        foreach (var methodName in new[] { "CreateCollectionIfNotExistsAsync", "CreateCollectionAsync", "CreateCollection" })
        {
            foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(x => x.Name == methodName))
            {
                try
                {
                    var parms = m.GetParameters();
                    object? result = null;
                    if (parms.Length == 1 && parms[0].ParameterType == typeof(CancellationToken))
                    {
                        result = m.Invoke(collection, new object[] { cancellationToken });
                    }
                    else if (parms.Length == 0)
                    {
                        result = m.Invoke(collection, null);
                    }
                    else
                    {
                        continue;
                    }
                    if (result is Task t) { await t.ConfigureAwait(false); }
                    return;
                }
                catch { }
            }
        }
    }

    public async Task<IEnumerable<(EntityVectorRecord Record, double Score)>> SearchAsync(ReadOnlyMemory<float> vector, int topK, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);
        if (vector.IsEmpty)
        {
            return Enumerable.Empty<(EntityVectorRecord, double)>();
        }

        using var perf = _performanceMonitor.StartOperation("Vector.Search", new Dictionary<string, object>
        {
            ["Collection"] = infrastructure.CollectionName,
            ["Provider"] = infrastructure.Provider,
            ["TopK"] = topK
        });

    var collection = _store.GetCollection<string, EntityVectorRecord>(infrastructure.CollectionName);
    await EnsureCollectionExistsOnCollectionAsync(collection!, cancellationToken).ConfigureAwait(false);
    await EnsureCollectionExistsAsync((VectorStoreCollection<string, EntityVectorRecord>)collection, cancellationToken).ConfigureAwait(false);

        var results = new List<(EntityVectorRecord, double)>();
        try
        {
            await foreach (var r in ((IVectorSearchable<EntityVectorRecord>)collection).SearchAsync(vector, topK, options: null, cancellationToken))
            {
                results.Add((r.Record, r.Score ?? 0d));
            }
        }
        catch (VectorStoreException ex) when (ex.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureCollectionExistsAsync((VectorStoreCollection<string, EntityVectorRecord>)collection, cancellationToken).ConfigureAwait(false);
            await EnsureCollectionExistsOnCollectionAsync(collection!, cancellationToken).ConfigureAwait(false);
            await foreach (var r in ((IVectorSearchable<EntityVectorRecord>)collection).SearchAsync(vector, topK, options: null, cancellationToken))
            {
                results.Add((r.Record, r.Score ?? 0d));
            }
        }
        return results;
    }
}
