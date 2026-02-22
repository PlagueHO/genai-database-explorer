using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Core.SemanticVectors.Policy;

public interface IVectorIndexPolicy
{
    /// <summary>
    /// Resolve the effective vector provider. When settings.Provider is "Auto",
    /// uses the repositoryStrategy hint (e.g., "LocalDisk", "AzureBlob", "CosmosDb").
    /// </summary>
    string ResolveProvider(VectorIndexSettings settings, string repositoryStrategy);

    /// <summary>
    /// Validate provider and settings compatibility against the repository strategy.
    /// </summary>
    void Validate(VectorIndexSettings settings, string repositoryStrategy);
}
