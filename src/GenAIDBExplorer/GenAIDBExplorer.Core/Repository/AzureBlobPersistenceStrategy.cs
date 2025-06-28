using System;
using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy that uses Azure Blob Storage.
    /// </summary>
    public class AzureBlobPersistenceStrategy : IAzureBlobPersistenceStrategy
    {
        public Task SaveModelAsync(SemanticModel semanticModel, DirectoryInfo modelPath)
        {
            throw new NotImplementedException("AzureBlobPersistenceStrategy.SaveModelAsync is not implemented.");
        }

        public Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
        {
            throw new NotImplementedException("AzureBlobPersistenceStrategy.LoadModelAsync is not implemented.");
        }

        public Task<bool> ExistsAsync(DirectoryInfo modelPath)
        {
            throw new NotImplementedException("AzureBlobPersistenceStrategy.ExistsAsync is not implemented.");
        }

        public Task<IEnumerable<string>> ListModelsAsync(DirectoryInfo rootPath)
        {
            throw new NotImplementedException("AzureBlobPersistenceStrategy.ListModelsAsync is not implemented.");
        }

        public Task DeleteModelAsync(DirectoryInfo modelPath)
        {
            throw new NotImplementedException("AzureBlobPersistenceStrategy.DeleteModelAsync is not implemented.");
        }
    }
}
