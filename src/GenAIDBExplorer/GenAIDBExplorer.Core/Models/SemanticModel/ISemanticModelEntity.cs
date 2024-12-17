namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a semantic model entity.
/// </summary>
public interface ISemanticModelEntity
{
    string Schema { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
    string? SemanticDescription { get; set; }
    DateTime? SemanticDescriptionLastUpdate { get; set; }
    bool NotUsed { get; set; }
    string? NotUsedReason { get; set; }
    Task SaveModelAsync(DirectoryInfo folderPath);
    Task LoadModelAsync(DirectoryInfo folderPath);
    FileInfo GetModelEntityFilename();
    DirectoryInfo GetModelPath();
    void SetSemanticDescription(string semanticDescription);
    /// <summary>
    /// Accepts a visitor to traverse the semantic model entity.
    /// </summary>
    /// <param name="visitor">The visitor that will be used to traverse the model entity.</param>
    void Accept(ISemanticModelVisitor visitor);
}
