using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticVectors.Policy;

namespace GenAIDBExplorer.Core.SemanticVectors.Infrastructure;

/// <summary>
/// Default factory that selects a provider using VectorIndexPolicy and returns minimal infrastructure.
/// </summary>
public sealed class VectorInfrastructureFactory(IVectorIndexPolicy policy) : IVectorInfrastructureFactory
{
    private readonly IVectorIndexPolicy _policy = policy;

    public VectorInfrastructure Create(VectorIndexSettings settings, string repositoryStrategy)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var provider = _policy.ResolveProvider(settings, repositoryStrategy);
        _policy.Validate(settings, repositoryStrategy);

        var collectionName = settings.CollectionName;
        var embeddingServiceId = settings.EmbeddingServiceId;

        return new VectorInfrastructure(provider, collectionName, embeddingServiceId, settings);
    }
}
