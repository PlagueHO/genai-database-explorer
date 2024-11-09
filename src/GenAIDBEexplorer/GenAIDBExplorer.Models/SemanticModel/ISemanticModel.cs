namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Represents a semantic model.
/// </summary>
internal interface ISemanticModel : ISemanticModelItem
{
    /// <summary>
    /// Gets the source of the semantic model.
    /// </summary>
    string Source { get; set; }

    /// <summary>
    /// Saves the semantic model to the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder path where the model will be saved.</param>
    void SaveModel(DirectoryInfo folderPath);
}
