using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy that uses existing local disk JSON operations.
    /// </summary>
    public class LocalDiskPersistenceStrategy : ILocalDiskPersistenceStrategy
    {
        public Task SaveModelAsync(SemanticModel semanticModel, DirectoryInfo modelPath)
        {
            return semanticModel.SaveModelAsync(modelPath);
        }

        public Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
        {
            // Instantiate a placeholder SemanticModel to invoke the instance loader
            var placeholder = new SemanticModel(string.Empty, string.Empty);
            return placeholder.LoadModelAsync(modelPath);
        }
    }
}
