using System.Text.Json;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a view in the semantic model.
/// </summary>
public sealed class SemanticModelView(
    string schema,
    string name,
    string? description = null
    ) : SemanticModelEntity(schema, name, description)
{
    /// <summary>
    /// Gets and sets the view definition.
    /// </summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// Gets the columns in the view.
    /// </summary>
    public List<SemanticModelColumn> Columns { get; set; } = [];

    /// <summary>
    /// Adds a column to the view.
    /// </summary>
    /// <param name="column">The column to add.</param>
    public void AddColumn(SemanticModelColumn column)
    {
        Columns.Add(column);
    }

    /// <summary>
    /// Removes a column from the view.
    /// </summary>
    /// <param name="column">The column to remove.</param>
    /// <returns>True if the column was removed; otherwise, false.</returns>
    public bool RemoveColumn(SemanticModelColumn column)
    {
        return Columns.Remove(column);
    }
}