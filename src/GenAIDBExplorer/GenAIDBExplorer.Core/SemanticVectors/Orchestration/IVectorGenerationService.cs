using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.SemanticVectors.Orchestration;

/// <summary>
/// Defines a service for generating semantic vector embeddings for a semantic model.
/// </summary>
public interface IVectorGenerationService
{
    /// <summary>
    /// Generates vector embeddings for the specified semantic model and writes results to the project path.
    /// </summary>
    /// <param name="model">The semantic model to process.</param>
    /// <param name="projectPath">The root directory of the project.</param>
    /// <param name="options">Options for vector generation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The number of processed entities.</returns>
    Task<int> GenerateAsync(SemanticModel model, DirectoryInfo projectPath, VectorGenerationOptions options, CancellationToken cancellationToken = default);
}
