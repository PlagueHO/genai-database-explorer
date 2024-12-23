using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a table in the semantic model.
/// </summary>
public sealed class SemanticModelTable(
    string schema,
    string name,
    string? description = null
    ) : SemanticModelEntity(schema, name, description), ISemanticModelColumnContainer, ISemanticModelIndexContainer
{
    /// <summary>
    /// Gets or sets the details of the purpose of the table.
    /// This is usually obtained from the data dictionary.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Details { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional information about the table (such as business rules).
    /// This is usually obtained from the data dictionary.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? AdditionalInformation { get; set; } = string.Empty;

    /// <summary>
    /// Gets the columns in the table.
    /// </summary>
    public List<SemanticModelColumn> Columns { get; set; } = [];

    /// <summary>
    /// Gets the indexes in the table.
    /// </summary>
    public List<SemanticModelIndex> Indexes { get; set; } = [];

    /// <summary>
    /// Adds a column to the table.
    /// </summary>
    /// <param name="column">The column to add.</param>
    public void AddColumn(SemanticModelColumn column)
    {
        Columns.Add(column);
    }

    /// <summary>
    /// Removes a column from the table.
    /// </summary>
    /// <param name="column">The column to remove.</param>
    /// <returns>True if the column was removed; otherwise, false.</returns>
    public bool RemoveColumn(SemanticModelColumn column)
    {
        return Columns.Remove(column);
    }

    /// <summary>
    /// Adds an index to the table.
    /// </summary>
    /// <param name="index">The index to add.</param>
    public void AddIndex(SemanticModelIndex index)
    {
        Indexes.Add(index);
    }

    /// <summary>
    /// Removes an index from the table.
    /// </summary>
    /// <param name="index">The index to remove.</param>
    /// <returns>True if the index was removed; otherwise, false.</returns>
    public bool RemoveIndex(SemanticModelIndex index)
    {
        return Indexes.Remove(index);
    }

    /// <inheritdoc/>
    public new async Task LoadModelAsync(DirectoryInfo folderPath)
    {
        var fileName = $"{Schema}.{Name}.json";
        var filePath = Path.Combine(folderPath.FullName, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The specified table file does not exist.", filePath);
        }

        await using var stream = File.OpenRead(filePath);
        var table = await JsonSerializer.DeserializeAsync<SemanticModelTable>(stream) ?? throw new InvalidOperationException("Failed to load table.");

        Schema = table.Schema;
        Name = table.Name;
        Details = table.Details;
        Description = table.Description;
        SemanticDescription = table.SemanticDescription;
        NotUsed = table.NotUsed;
        NotUsedReason = table.NotUsedReason;
        Columns = table.Columns;
        Indexes = table.Indexes;
        AdditionalInformation = table.AdditionalInformation;
    }

    /// <inheritdoc/>
    public override DirectoryInfo GetModelPath()
    {
        return new DirectoryInfo(Path.Combine("tables", GetModelEntityFilename().Name));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(base.ToString());

        if (!string.IsNullOrWhiteSpace(Details))
        {
            builder.AppendLine("");
            builder.AppendLine($"Details:");
            builder.AppendLine($"{Details}");
        }

        if (Columns.Count > 0)
        {
            builder.AppendLine("");
            builder.AppendLine("Columns:");
            foreach (var column in Columns)
            {
                builder.AppendLine($"  - {column.Name} ({column.Type})");
                if (!string.IsNullOrWhiteSpace(column.Description)) builder.AppendLine($"    Description: {column.Description}");
                if (column.IsPrimaryKey) builder.AppendLine("    Primary Key");
                if (column.IsNullable) builder.AppendLine("    Nullable");
                if (column.IsIdentity) builder.AppendLine("    Identity");
                if (column.IsComputed) builder.AppendLine("    Computed");
                if (column.IsXmlDocument) builder.AppendLine("    XML Document");
                if (column.MaxLength.HasValue) builder.AppendLine($"    Max Length: {column.MaxLength}");
                if (column.Precision.HasValue) builder.AppendLine($"    Precision: {column.Precision}");
                if (column.Scale.HasValue) builder.AppendLine($"    Scale: {column.Scale}");
                if (!string.IsNullOrWhiteSpace(column.ReferencedTable)) builder.AppendLine($"    References: {column.ReferencedTable}({column.ReferencedColumn})");
            }
        }

        if (Indexes.Count > 0)
        {
            builder.AppendLine("");
            builder.AppendLine("Indexes:");
            foreach (var index in Indexes)
            {
                builder.AppendLine($"  - {index.Name}");
            }
        }

        if (!string.IsNullOrEmpty(AdditionalInformation))
        {
            builder.AppendLine("");
            builder.AppendLine($"Additional Information:");
            builder.AppendLine($"{AdditionalInformation}");
        }

        if (!string.IsNullOrWhiteSpace(SemanticDescription))
        {
            builder.AppendLine("");
            builder.AppendLine($"Semantic Description:");
            builder.AppendLine($"{SemanticDescription}");
        }

        return builder.ToString();
    }

    /// <inheritdoc/>
    public override void Accept(ISemanticModelVisitor visitor)
    {
        visitor.VisitTable(this);
        foreach (var column in Columns)
        {
            column.Accept(visitor);
        }
    }
}
