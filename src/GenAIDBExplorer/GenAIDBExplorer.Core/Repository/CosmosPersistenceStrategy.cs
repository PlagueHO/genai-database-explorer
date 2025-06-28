using System;
using System.IO;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy that uses Cosmos DB.
    /// </summary>
    public class CosmosPersistenceStrategy : ICosmosPersistenceStrategy
    {
        public Task SaveModelAsync(SemanticModel semanticModel, DirectoryInfo modelPath)
        {
            throw new NotImplementedException("CosmosPersistenceStrategy.SaveModelAsync is not implemented.");
        }

        public Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
        {
            throw new NotImplementedException("CosmosPersistenceStrategy.LoadModelAsync is not implemented.");
        }
    }
}
