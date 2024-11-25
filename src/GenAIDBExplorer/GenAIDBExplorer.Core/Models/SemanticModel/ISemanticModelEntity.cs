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
    bool IsIgnored { get; set; }
    string? IgnoreReason { get; set; }
    Task SaveModelAsync(DirectoryInfo folderPath);
    Task LoadModelAsync(DirectoryInfo folderPath);
    FileInfo GetModelEntityFilename();
    DirectoryInfo GetModelPath();
    void SetSemanticDescription(string semanticDescription);
}
