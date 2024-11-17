namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a semantic model.
/// </summary>
public interface ISemanticModel
{
    /// <summary>
    /// Gets the source of the semantic model.
    /// </summary>
    string Source { get; set; }

    /// <summary>
    /// Saves the semantic model to the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the semantic model will be saved.</param>
    /// <param name="splitModel">Flag to split the model into separate files.</param>
    public Task SaveModelAsync(DirectoryInfo modelPath, bool splitModel = false);
}
