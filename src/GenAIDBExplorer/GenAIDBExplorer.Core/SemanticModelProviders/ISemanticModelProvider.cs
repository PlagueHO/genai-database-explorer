using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.SemanticModelProviders;

public interface ISemanticModelProvider
{
    /// <summary>
    /// Creates a new empty semantic model, configured with the project information.
    /// </summary>
    /// <returns>Returns the empty configured <see cref="SemanticModel"/>.</returns>
    SemanticModel CreateSemanticModel();

    /// <summary>
    /// Loads an existing semantic model asynchronously.
    /// </summary>
    /// <param name="modelPath">The folder path where the model is located.</param>
    /// <returns></returns>
    Task<SemanticModel> LoadSemanticModelAsync(DirectoryInfo modelPath);

    /// <summary>
    /// Extracts the semantic model from the SQL database asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains the built <see cref="SemanticModel"/>.</returns>
    Task<SemanticModel> ExtractSemanticModelAsync();
}