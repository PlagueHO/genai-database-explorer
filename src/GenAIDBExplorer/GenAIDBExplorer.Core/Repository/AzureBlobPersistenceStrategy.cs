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
    }
}
