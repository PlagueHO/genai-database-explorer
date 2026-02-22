using System;
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
public sealed class SkInMemoryVectorSearchService : IVectorSearchService
{
    private readonly InMemoryVectorStore? _store;
    private readonly IVectorStoreAdapter? _adapter;
    private readonly IPerformanceMonitor _performanceMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkInMemoryVectorSearchService"/> class with a direct store reference.
    /// </summary>
    /// <param name="store">The InMemoryVectorStore instance to use for vector operations.</param>
    /// <param name="performanceMonitor">The performance monitor for tracking operation metrics.</param>
    public SkInMemoryVectorSearchService(InMemoryVectorStore store, IPerformanceMonitor performanceMonitor)
    {
        _store = store;
        _performanceMonitor = performanceMonitor;
        _adapter = new InMemoryVectorStoreAdapter(store);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkInMemoryVectorSearchService"/> class with an adapter.
    /// </summary>
    /// <param name="adapter">The vector store adapter to use for vector operations.</param>
    /// <param name="performanceMonitor">The performance monitor for tracking operation metrics.</param>
    public SkInMemoryVectorSearchService(IVectorStoreAdapter adapter, IPerformanceMonitor performanceMonitor)
    {
        _adapter = adapter;
        _performanceMonitor = performanceMonitor;
    }

    /// <summary>
    /// Ensures that the specified vector store collection exists by calling its EnsureCollectionExistsAsync method.
    /// </summary>
    /// <param name="collection">The vector store collection to ensure exists.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static Task EnsureCollectionExistsAsync(VectorStoreCollection<string, EntityVectorRecord> collection, CancellationToken cancellationToken)
        => collection.EnsureCollectionExistsAsync(cancellationToken);

    /// <summary>
    /// Ensures that a collection with the specified name exists on the vector store using reflection to call
    /// the appropriate creation method. This method tries various method names and signatures to accommodate
    /// different versions of the Semantic Kernel API.
    /// </summary>
    /// <param name="store">The InMemoryVectorStore instance.</param>
    /// <param name="collectionName">The name of the collection to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Ensures that a collection exists by calling collection-level creation methods using reflection.
    /// This method tries various method names and signatures to accommodate different versions of the
    /// Semantic Kernel API and provides a fallback mechanism for collection creation.
    /// </summary>
    /// <param name="collection">The collection object on which to call the creation method.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Performs a vector similarity search to find the most relevant entity vector records.
    /// This method searches the specified collection using the provided vector and returns the top K results
    /// ordered by similarity score. It includes automatic collection creation and retry logic for robustness.
    /// </summary>
    /// <param name="vector">The query vector to search with. Must not be empty.</param>
    /// <param name="topK">The maximum number of results to return. Must be greater than zero.</param>
    /// <param name="infrastructure">The vector infrastructure configuration containing collection details.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous search operation. The result contains an enumerable of tuples,
    /// where each tuple contains an EntityVectorRecord and its similarity score.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="topK"/> is zero or negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the collection cannot be retrieved from the vector store.</exception>
    /// <exception cref="VectorStoreException">Thrown when vector store operations fail after retry attempts.</exception>
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

        var collection = (_adapter != null)
            ? _adapter.GetCollection<string, EntityVectorRecord>(infrastructure.CollectionName)
            : _store!.GetCollection<string, EntityVectorRecord>(infrastructure.CollectionName);

        if (collection == null)
        {
            throw new InvalidOperationException($"Failed to get collection '{infrastructure.CollectionName}' from vector store.");
        }

        await EnsureCollectionExistsOnCollectionAsync(collection, cancellationToken).ConfigureAwait(false);
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
            await EnsureCollectionExistsOnCollectionAsync(collection, cancellationToken).ConfigureAwait(false);
            await foreach (var r in ((IVectorSearchable<EntityVectorRecord>)collection).SearchAsync(vector, topK, options: null, cancellationToken))
            {
                results.Add((r.Record, r.Score ?? 0d));
            }
        }
        return results;
    }
}
