using System;
using Microsoft.SemanticKernel.Connectors.InMemory;
using System.Threading;

namespace GenAIDBExplorer.Core.SemanticVectors.Search;

/// <summary>
/// Provides an adapter implementation for InMemoryVectorStore that implements the IVectorStoreAdapter interface.
/// This adapter allows for consistent access to vector store collections through a common interface,
/// enabling easier testing and abstraction of vector store operations.
/// </summary>
public sealed class InMemoryVectorStoreAdapter : IVectorStoreAdapter
{
    private readonly InMemoryVectorStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryVectorStoreAdapter"/> class.
    /// </summary>
    /// <param name="store">The InMemoryVectorStore instance to wrap and provide adapter functionality for.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public InMemoryVectorStoreAdapter(InMemoryVectorStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }
    /// <summary>
    /// Retrieves a collection from the underlying InMemoryVectorStore with the specified name and types.
    /// This method provides a generic way to access vector store collections while maintaining type safety.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used to identify records in the collection. Must be non-null.</typeparam>
    /// <typeparam name="TItem">The type of items stored in the collection. Must be a reference type.</typeparam>
    /// <param name="collectionName">The name of the collection to retrieve.</param>
    /// <returns>
    /// The collection object if found, or null if the collection does not exist or cannot be retrieved.
    /// The returned object should be cast to the appropriate collection type for use.
    /// </returns>
    public object? GetCollection<TKey, TItem>(string collectionName)
        where TKey : notnull
        where TItem : class
    {
        return _store.GetCollection<TKey, TItem>(collectionName);
    }
}
