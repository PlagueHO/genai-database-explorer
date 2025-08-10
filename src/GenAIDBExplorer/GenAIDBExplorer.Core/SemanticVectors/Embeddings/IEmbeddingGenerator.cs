using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;

namespace GenAIDBExplorer.Core.SemanticVectors.Embeddings;

public interface IEmbeddingGenerator
{
    /// <summary>
    /// Generate an embedding vector for the supplied text using the embedding service configured in VectorInfrastructure.
    /// </summary>
    Task<ReadOnlyMemory<float>> GenerateAsync(string text, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default);
}
