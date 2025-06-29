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
    }
}
