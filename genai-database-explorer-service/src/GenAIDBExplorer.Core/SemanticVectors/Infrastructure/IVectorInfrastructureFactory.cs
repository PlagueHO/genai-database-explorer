using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Core.SemanticVectors.Infrastructure;

/// <summary>
/// Builds provider-specific vector infrastructure (collection/index identifiers and embedding service id).
/// This is a lightweight abstraction to keep dependencies minimal for Phase 3 scaffolding.
/// </summary>
public interface IVectorInfrastructureFactory
{
    VectorInfrastructure Create(VectorIndexSettings settings, string repositoryStrategy);
}

/// <summary>
/// Minimal description of the vector infrastructure required by downstream services.
/// </summary>
public sealed record VectorInfrastructure(
    string Provider,
    string CollectionName,
    string EmbeddingServiceId,
    VectorIndexSettings Settings
);
