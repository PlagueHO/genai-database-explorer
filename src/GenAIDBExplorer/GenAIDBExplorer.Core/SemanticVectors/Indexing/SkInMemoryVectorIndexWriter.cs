using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.Extensions.VectorData;
using System.Reflection;
using GenAIDBExplorer.Core.Repository.Performance;

namespace GenAIDBExplorer.Core.SemanticVectors.Indexing;

/// <summary>
/// Vector index writer backed by Semantic Kernel's InMemoryVectorStore.
/// Uses a single static store for process lifetime to simulate a shared in-memory index for tests/dev.
/// </summary>
public sealed class SkInMemoryVectorIndexWriter(InMemoryVectorStore store, IPerformanceMonitor performanceMonitor) : IVectorIndexWriter
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
                        // Prefer <TRecord>
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
                catch
                {
                    // ignore and try next overload
                }
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

    public async Task UpsertAsync(EntityVectorRecord record, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(infrastructure);

        using var perf = _performanceMonitor.StartOperation("Vector.Index.Upsert", new Dictionary<string, object>
        {
            ["Collection"] = infrastructure.CollectionName,
            ["Provider"] = infrastructure.Provider
        });

        // Ensure collection exists via collection API (new VectorStore API)
        var collection = _store.GetCollection<string, EntityVectorRecord>(infrastructure.CollectionName);
        await EnsureCollectionExistsAsync((VectorStoreCollection<string, EntityVectorRecord>)collection, cancellationToken).ConfigureAwait(false);
        await EnsureCollectionExistsOnCollectionAsync(collection!, cancellationToken).ConfigureAwait(false);
        try
        {
            await collection.UpsertAsync(record, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (VectorStoreException ex) when (ex.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            // Race or older SK version – create and retry once
            await EnsureCollectionExistsAsync((VectorStoreCollection<string, EntityVectorRecord>)collection, cancellationToken).ConfigureAwait(false);
            await EnsureCollectionExistsOnCollectionAsync(collection!, cancellationToken).ConfigureAwait(false);
            await collection.UpsertAsync(record, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
