namespace GenAIDBExplorer.Core.Models.SemanticModel.Export;

/// <summary>
/// Defines an interface for exporting the semantic model.
/// </summary>
public interface IExportStrategy
{
    Task ExportAsync(SemanticModel semanticModel, ExportOptions options);
}
