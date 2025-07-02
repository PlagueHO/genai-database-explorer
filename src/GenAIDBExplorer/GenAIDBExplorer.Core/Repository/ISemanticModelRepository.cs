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
        /// Loads the semantic model using the specified persistence strategy.
        /// </summary>
        Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, string? strategyName = null);

        /// <summary>
        /// Loads the semantic model with optional lazy loading for entity collections.
        /// </summary>
        /// <param name="modelPath">The path to load the model from.</param>
        /// <param name="enableLazyLoading">Whether to enable lazy loading for entity collections.</param>
        /// <param name="strategyName">Optional strategy name to use for loading.</param>
        Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, bool enableLazyLoading, string? strategyName = null);

        /// <summary>
        /// Loads the semantic model with optional lazy loading and change tracking.
        /// </summary>
        /// <param name="modelPath">The path to load the model from.</param>
        /// <param name="enableLazyLoading">Whether to enable lazy loading for entity collections.</param>
        /// <param name="enableChangeTracking">Whether to enable change tracking for the model.</param>
        /// <param name="strategyName">Optional strategy name to use for loading.</param>
        Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, bool enableLazyLoading, bool enableChangeTracking, string? strategyName = null);

        /// <summary>
        /// Loads the semantic model with optional lazy loading, change tracking, and caching.
        /// </summary>
        /// <param name="modelPath">The path to load the model from.</param>
        /// <param name="enableLazyLoading">Whether to enable lazy loading for entity collections.</param>
        /// <param name="enableChangeTracking">Whether to enable change tracking for the model.</param>
        /// <param name="enableCaching">Whether to use caching for loading and storing the model.</param>
        /// <param name="strategyName">Optional strategy name to use for loading.</param>
        Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, bool enableLazyLoading, bool enableChangeTracking, bool enableCaching, string? strategyName = null);
    }
}
