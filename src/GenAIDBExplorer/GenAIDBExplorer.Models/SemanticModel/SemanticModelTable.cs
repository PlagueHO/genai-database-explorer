namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a table in the semantic model.
/// </summary>
public sealed class SemanticModelTable(
    string schema,
    string name,
    string? description = null
    ) : SemanticModelEntity(schema, name, description)
{
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
    public override DirectoryInfo GetModelPath()
    {
        return new DirectoryInfo(Path.Combine("tables", GetModelEntityFilename().Name));
    }
}
