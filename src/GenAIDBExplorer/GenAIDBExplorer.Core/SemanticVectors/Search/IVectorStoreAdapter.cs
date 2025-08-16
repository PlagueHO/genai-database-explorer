using Microsoft.SemanticKernel.Connectors.InMemory;

namespace GenAIDBExplorer.Core.SemanticVectors.Search;

public interface IVectorStoreAdapter
{
    object? GetCollection<TKey, TItem>(string collectionName)
        where TKey : notnull
        where TItem : class;
}
