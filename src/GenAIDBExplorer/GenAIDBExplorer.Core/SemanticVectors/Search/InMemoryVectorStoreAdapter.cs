using Microsoft.SemanticKernel.Connectors.InMemory;
using System.Threading;

namespace GenAIDBExplorer.Core.SemanticVectors.Search;

public sealed class InMemoryVectorStoreAdapter : IVectorStoreAdapter
{
    private readonly InMemoryVectorStore _store;
    public InMemoryVectorStoreAdapter(InMemoryVectorStore store)
    {
        _store = store;
    }
    public object? GetCollection<TKey, TItem>(string collectionName)
        where TKey : notnull
        where TItem : class
    {
        return _store.GetCollection<TKey, TItem>(collectionName);
    }
}
