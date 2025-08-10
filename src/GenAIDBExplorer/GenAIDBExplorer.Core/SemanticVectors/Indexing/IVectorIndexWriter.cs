using System.Threading;
using System.Threading.Tasks;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;

namespace GenAIDBExplorer.Core.SemanticVectors.Indexing;

public interface IVectorIndexWriter
{
    Task UpsertAsync(EntityVectorRecord record, VectorInfrastructure infrastructure, CancellationToken cancellationToken = default);
}
