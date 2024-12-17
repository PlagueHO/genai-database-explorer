namespace GenAIDBExplorer.Core.Export;

using GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Defines an interface for exporting the semantic model.
/// </summary>
public interface IExportStrategy
{
    Task ExportAsync(SemanticModel semanticModel, ExportOptions options);
}
