using System.IO;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy for Cosmos DB JSON documents.
    /// </summary>
    public interface ICosmosPersistenceStrategy : ISemanticModelPersistenceStrategy
    {
        // Additional Cosmos-specific members can be added here.
    }
}
