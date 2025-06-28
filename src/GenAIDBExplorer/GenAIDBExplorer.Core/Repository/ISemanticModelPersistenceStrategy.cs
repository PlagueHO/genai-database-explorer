using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Defines persistence operations for semantic models.
    /// </summary>
    public interface ISemanticModelPersistenceStrategy
    {
        /// <summary>
        /// Saves the semantic model to the specified path.
        /// </summary>
        Task SaveModelAsync(SemanticModel semanticModel, DirectoryInfo modelPath);

        /// <summary>
        /// Loads the semantic model from the specified path.
        /// </summary>
        Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath);
    }
}
