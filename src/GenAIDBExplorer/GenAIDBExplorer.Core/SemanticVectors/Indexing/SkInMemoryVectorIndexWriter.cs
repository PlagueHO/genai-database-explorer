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
    private static async Task EnsureCollectionExistsAsync(object collection, CancellationToken cancellationToken)
    {
        var type = collection.GetType();
        // Try CreateCollectionIfNotExistsAsync()
        var method = type.GetMethod("CreateCollectionIfNotExistsAsync", BindingFlags.Instance | BindingFlags.Public, new Type[] { });
        if (method is not null)
        {
            if (method.Invoke(collection, null) is Task t1) { await t1.ConfigureAwait(false); }
            return;
        }
        // Try CreateCollectionIfNotExistsAsync(CancellationToken)
        method = type.GetMethod("CreateCollectionIfNotExistsAsync", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(CancellationToken) });
        if (method is not null)
        {
            if (method.Invoke(collection, new object[] { cancellationToken }) is Task t2) { await t2.ConfigureAwait(false); }
            return;
        }
        // Try CreateCollectionAsync()
        method = type.GetMethod("CreateCollectionAsync", BindingFlags.Instance | BindingFlags.Public, new Type[] { });
        if (method is not null)
        {
            if (method.Invoke(collection, null) is Task t3) { await t3.ConfigureAwait(false); }
            return;
        }
        // Try CreateCollectionAsync(CancellationToken)
        method = type.GetMethod("CreateCollectionAsync", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(CancellationToken) });
        if (method is not null)
        {
            if (method.Invoke(collection, new object[] { cancellationToken }) is Task t4) { await t4.ConfigureAwait(false); }
            return;
        }
        // Nothing to do if none found
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

        // Ensure collection exists; schema is provided via attributes on EntityVectorRecord.
        var collection = _store.GetCollection<string, EntityVectorRecord>(infrastructure.CollectionName);
        await EnsureCollectionExistsAsync(collection!, cancellationToken).ConfigureAwait(false);
        await collection.UpsertAsync(record, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
