using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace GenAIDBExplorer.Core.Models.SemanticModel.YamlConverters;

public class SemanticModelTableYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return typeof(SemanticModelTable).IsAssignableFrom(type);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer deserializer)
    {
        // Deserialization is not required for this use case
        throw new NotImplementedException("Deserialization is not implemented.");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not SemanticModelTable table)
        {
            throw new ArgumentException("Expected a SemanticModelTable object.", nameof(value));
        }

        // Filter out columns where NotUsed is true
        var filteredColumns = table.Columns
            .Where(column => !column.NotUsed)
            .ToList();

        // Create a clone of the table with filtered columns
        var tableClone = CloneSemanticModelTableWithFilteredColumns(table, filteredColumns);

        // Use the provided serializer to serialize the cloned table
        serializer(tableClone, type);
    }

    private static SemanticModelTable CloneSemanticModelTableWithFilteredColumns(
        SemanticModelTable originalTable,
        List<SemanticModelColumn> filteredColumns)
    {
        // Manually create a new instance and copy properties
        var tableClone = new SemanticModelTable(
            schema: originalTable.Schema,
            name: originalTable.Name,
            description: originalTable.Description)
        {
            Details = originalTable.Details,
            AdditionalInformation = originalTable.AdditionalInformation,
            Columns = filteredColumns,
            Indexes = originalTable.Indexes,
            SemanticDescription = originalTable.SemanticDescription,
            SemanticDescriptionLastUpdate = originalTable.SemanticDescriptionLastUpdate,
            NotUsed = originalTable.NotUsed,
            NotUsedReason = originalTable.NotUsedReason
        };

        return tableClone;
    }
}
