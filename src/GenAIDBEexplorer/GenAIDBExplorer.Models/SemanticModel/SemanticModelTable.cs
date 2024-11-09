using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a table in the semantic model.
/// </summary>
public sealed class SemanticModelTable(
    string schema,
    string name,
    string? description = null
    ) : ISemanticModelItem
{
    /// <summary>
    /// Gets the schema of the table.
    /// </summary>
    public string Schema { get; set; } = schema;

    /// <summary>
    /// Gets the name of the table.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets the description of the table.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; } = description;

    /// <summary>
    /// Gets the columns in the table.
    /// </summary>
    public List<SemanticModelColumn> Columns { get; set; } = [];

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
}