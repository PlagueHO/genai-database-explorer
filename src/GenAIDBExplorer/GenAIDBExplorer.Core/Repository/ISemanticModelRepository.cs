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
    }
}
