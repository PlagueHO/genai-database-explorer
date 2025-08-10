using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.Extensions.VectorData;

namespace GenAIDBExplorer.Core.SemanticVectors.Indexing;

/// <summary>
/// Vector index writer backed by Semantic Kernel's InMemoryVectorStore.
/// Uses a single static store for process lifetime to simulate a shared in-memory index for tests/dev.
/// </summary>
public sealed class SkInMemoryVectorIndexWriter(InMemoryVectorStore store) : IVectorIndexWriter
{
    private readonly InMemoryVectorStore _store = store;

    public async Task UpsertAsync(EntityVectorRecord record, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(infrastructure);

        // Schema is provided via attributes on EntityVectorRecord.
        var collection = _store.GetCollection<string, EntityVectorRecord>(infrastructure.CollectionName);
        await collection.UpsertAsync(record, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
