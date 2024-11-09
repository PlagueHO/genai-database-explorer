using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a column in the semantic model.
/// </summary>
public sealed class SemanticModelColumn(
    string name,
    string type,
    bool isPrimaryKey,
    string? description = null,
    string? referencedTable = null,
    string? referencedColumn = null
    ) : ISemanticModelItem
{
    /// <summary>
    /// Gets the name of the column.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets the description of the column.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; } = description;

    /// <summary>
    /// Gets the type of the column.
    /// </summary>
    public string Type { get; set; } = type;

    /// <summary>
    /// Gets a value indicating whether the column is a primary key.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsPrimaryKey { get; set; } = isPrimaryKey;

    /// <summary>
    /// Gets the name of the referenced table, if any.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ReferencedTable { get; set; } = referencedTable;

    /// <summary>
    /// Gets the name of the referenced column, if any.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ReferencedColumn { get; set; } = referencedColumn;
}