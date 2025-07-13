using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Repository abstraction for semantic model persistence.
    /// </summary>
    public interface ISemanticModelRepository
    {
        /// <summary>
        /// Saves the semantic model using the specified persistence strategy.
        /// </summary>
        Task SaveModelAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null);

        /// <summary>
        /// Saves only the changes (dirty entities) in the semantic model if change tracking is enabled.
        /// Falls back to full save if change tracking is not enabled or no changes are detected.
        /// </summary>
        Task SaveChangesAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null);

        /// <summary>
        /// Loads the semantic model using default options (no lazy loading, change tracking, or caching).
        /// </summary>
        Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, string? strategyName = null);

        /// <summary>
        /// Loads the semantic model using immutable options configuration.
        /// This method provides a fluent, thread-safe alternative to boolean parameter overloads.
        /// </summary>
        /// <param name="modelPath">The path to load the model from.</param>
        /// <param name="options">Immutable options configuration created via the builder pattern.</param>
        Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, SemanticModelRepositoryOptions options);
    }
}
