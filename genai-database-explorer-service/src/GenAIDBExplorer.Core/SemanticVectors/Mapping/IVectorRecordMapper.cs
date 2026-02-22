using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticVectors.Records;

namespace GenAIDBExplorer.Core.SemanticVectors.Mapping;

public interface IVectorRecordMapper
{
    string BuildEntityText(SemanticModelEntity entity);
    EntityVectorRecord ToRecord(SemanticModelEntity entity, string id, string content, ReadOnlyMemory<float> vector, string contentHash);
}
