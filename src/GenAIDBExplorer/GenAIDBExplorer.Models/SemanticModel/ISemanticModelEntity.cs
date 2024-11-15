namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a semantic model entity.
/// </summary>
public interface ISemanticModelEntity
{
    /// <summary>
    /// Gets the schema of the semantic model entity.
    /// </summary>
    string Schema { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the entity.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the semantic description of the entity.
    /// </summary>
    public string? SemanticDescription { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity should be ignored.
    /// </summary>
    public bool IsIgnored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating why the entity is ignored (if ignored).
    /// </summary>
    public string? IgnoreReason { get; set; }

    /// <summary>
    /// Saves the semantic model entity to the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder path where the entity will be saved.</param>
    public Task SaveModelAsync(DirectoryInfo folderPath);

    /// <summary>
    /// Loads the semantic model entity from the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder path where the entity will be loaded from.</param>
    public Task LoadModelAsync(DirectoryInfo folderPath);

    /// <summary>
    /// Gets the filename of the model entity if the model is split.
    /// </summary>
    /// <returns>The filename of the model entity.</returns>
    public FileInfo GetModelEntityFilename();

    /// <summary>
    /// Gets the path to the model entity if the model is split.
    /// </summary>
    /// <returns>The relative path to the model entity.</returns>
    public DirectoryInfo GetModelPath();
}
