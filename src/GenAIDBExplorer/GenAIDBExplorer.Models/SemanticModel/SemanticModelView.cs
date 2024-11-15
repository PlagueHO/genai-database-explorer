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

    /// <inheritdoc/>
    public new async Task LoadModelAsync(DirectoryInfo folderPath)
    {
        var fileName = $"{Schema}.{Name}.json";
        var filePath = Path.Combine(folderPath.FullName, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The specified view file does not exist.", filePath);
        }
        
        var json = await File.ReadAllTextAsync(filePath);
        var view = JsonSerializer.Deserialize<SemanticModelView>(json) ?? throw new InvalidOperationException("Failed to load view.");

        Schema = view.Schema;
        Name = view.Name;
        Description = view.Description;
        SemanticDescription = view.SemanticDescription;
        IsIgnored = view.IsIgnored;
        IgnoreReason = view.IgnoreReason;
        Definition = view.Definition;
        Columns = view.Columns;
    }

    /// <inheritdoc/>
    public override DirectoryInfo GetModelPath()
    {
        return new DirectoryInfo(Path.Combine("views", GetModelEntityFilename().Name));
    }

}