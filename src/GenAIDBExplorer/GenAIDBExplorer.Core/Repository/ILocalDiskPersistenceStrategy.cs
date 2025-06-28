using GenAIDBExplorer.Core.Models.SemanticModel;
using System.IO;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy for local disk JSON files.
    /// </summary>
    public interface ILocalDiskPersistenceStrategy : ISemanticModelPersistenceStrategy
    {
        // Additional local-disk-specific members can be added here.
    }
}
