using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.SemanticVectors.Orchestration;

/// <summary>
/// Orchestrates the process of generating and managing semantic vectors for a semantic model.
/// </summary>
public interface IVectorOrchestrator
{
    /// <summary>
    /// Runs the orchestration process for vector generation on the given semantic model.
    /// </summary>
    /// <param name="model">The semantic model to process.</param>
    /// <param name="projectPath">The root directory of the project.</param>
    /// <param name="options">Options for vector generation orchestration.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The number of processed entities.</returns>
    Task<int> GenerateAsync(SemanticModel model, DirectoryInfo projectPath, VectorGenerationOptions options, CancellationToken cancellationToken = default);
}
