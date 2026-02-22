using System.IO;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Persistence strategy for Azure Blob Storage JSON files.
    /// </summary>
    public interface IAzureBlobPersistenceStrategy : ISemanticModelPersistenceStrategy, IDisposable
    {
        // Additional Azure Blob-specific members can be added here.
    }
}
