using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GenAIDBExplorer.Core.Models.SemanticModel.Extensions;

/// <summary>
/// Provides extension methods for converting SemanticModelEntities to YAML.
/// </summary>
public static class SemanticModelEntityExtensions
{
    public static string ToYaml(this ISemanticModelEntity entity)
    {
        // Call the appropriate ToYaml method based on the entity type
        return entity switch
        {
            SemanticModelTable table => table.ToYaml(),
            SemanticModelView view => view.ToYaml(),
            SemanticModelStoredProcedure storedProcedure => storedProcedure.ToYaml(),
            _ => throw new NotSupportedException($"Unsupported entity type: {entity.GetType()}")
        };
    }

    /// <summary>
    /// Converts the <see cref="SemanticModelTable"/> to a YAML string,
    /// including only columns where NotUsed is false.
    /// </summary>
    /// <param name="table">The semantic model table to convert.</param>
    /// <returns>A YAML string representation of the table.</returns>
    public static string ToYaml(this SemanticModelTable table)
    {
        var filteredTable = table.WithoutNotUsedColumns();

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

        return serializer.Serialize(filteredTable);
    }

    /// <summary>
    /// Creates a copy of the table, excluding columns where NotUsed is true.
    /// </summary>
    /// <param name="table">The original semantic model table.</param>
    /// <returns>A new <see cref="SemanticModelTable"/> with filtered columns.</returns>
    public static SemanticModelTable WithoutNotUsedColumns(this SemanticModelTable table)
    {
        // Create a new instance of SemanticModelTable with copied properties
        var filteredTable = new SemanticModelTable(table.Schema, table.Name, table.Description)
        {
            Details = table.Details,
            AdditionalInformation = table.AdditionalInformation,
            Columns = table.Columns
                .Where(column => !column.NotUsed)
                .ToList(),
            Indexes = table.Indexes
        };

        return filteredTable;
    }

    /// <summary>
    /// Converts the <see cref="SemanticModelView"/> to a YAML string,
    /// including only columns where NotUsed is false.
    /// </summary>
    /// <param name="view">The semantic model view to convert.</param>
    /// <returns>A YAML string representation of the view.</returns>
    public static string ToYaml(this SemanticModelView view)
    {
        var filteredView = view.WithoutNotUsedColumns();

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

        return serializer.Serialize(filteredView);
    }

    /// <summary>
    /// Creates a copy of the view, excluding columns where NotUsed is true.
    /// </summary>
    /// <param name="view">The original semantic model view.</param>
    /// <returns>A new <see cref="SemanticModelView"/> with filtered columns.</returns>
    public static SemanticModelView WithoutNotUsedColumns(this SemanticModelView view)
    {
        // Create a new instance of SemanticModelView with copied properties
        var filteredView = new SemanticModelView(view.Schema, view.Name, view.Description)
        {
            AdditionalInformation = view.AdditionalInformation,
            Definition = view.Definition,
            Columns = view.Columns
                .Where(column => !column.NotUsed)
                .ToList()
        };

        return filteredView;
    }

    /// <summary>
    /// Converts the <see cref="SemanticModelStoredProcedure"/> to a YAML string.
    /// </summary>
    /// <param name="storedProcedure">The semantic model stored procedure to convert.</param>
    /// <returns>A YAML string representation of the stored procedure.</returns>
    public static string ToYaml(this SemanticModelStoredProcedure storedProcedure)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

        return serializer.Serialize(storedProcedure);
    }
}