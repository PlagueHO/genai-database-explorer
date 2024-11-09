// File: GenAIDBExplorer.Models/SemanticModel/ISemanticModelEntity.cs

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a semantic model entity.
/// </summary>
internal interface ISemanticModelEntity
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
    /// Saves the semantic model entity to the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder path where the entity will be saved.</param>
    void SaveModel(DirectoryInfo folderPath);
}

