using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;

namespace GenAIDBExplorer.Core.SemanticVectors.Indexing;

public class InMemoryVectorIndexWriter : IVectorIndexWriter
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, EntityVectorRecord>> _store = new();

    public Task UpsertAsync(EntityVectorRecord record, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(infrastructure);

        var collection = _store.GetOrAdd(infrastructure.CollectionName, _ => new());
        collection[record.Id] = record;
        return Task.CompletedTask;
    }

    public static IReadOnlyDictionary<string, EntityVectorRecord> GetCollection(string collectionName)
        => _store.TryGetValue(collectionName, out var coll) ? coll : new();
}
