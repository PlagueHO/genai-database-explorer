using System.Text;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticVectors.Records;

namespace GenAIDBExplorer.Core.SemanticVectors.Mapping;

public sealed class VectorRecordMapper : IVectorRecordMapper
{
    public string BuildEntityText(SemanticModelEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var sb = new StringBuilder();
    sb.AppendLine($"Schema: {entity.Schema}");
        sb.AppendLine($"Name: {entity.Name}");
        if (!string.IsNullOrWhiteSpace(entity.Description))
        {
            sb.AppendLine("Description:");
            sb.AppendLine(entity.Description);
        }
        // Include basic fields if available (columns, parameters, etc.)
    if (entity is SemanticModelTable table && table.Columns?.Count > 0)
        {
            sb.AppendLine("Columns:");
            foreach (var c in table.Columns)
            {
        sb.AppendLine($"- {c.Name}: {c.Type} {(string.IsNullOrWhiteSpace(c.Description) ? string.Empty : "- " + c.Description)}");
            }
        }
        return sb.ToString();
    }

    public EntityVectorRecord ToRecord(SemanticModelEntity entity, string id, string content, ReadOnlyMemory<float> vector, string contentHash)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new EntityVectorRecord
        {
            Id = id,
            Content = content,
            Vector = vector,
            Schema = entity.Schema,
            EntityType = entity.GetType().Name,
            Name = entity.Name,
            ContentHash = contentHash
        };
    }
}
