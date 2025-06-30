namespace GenAIDBExplorer.Core.Models.SemanticModel.Export;

/// <summary>
/// Represents options for exporting the semantic model.
/// </summary>
public class ExportOptions
{
    public required DirectoryInfo ProjectPath { get; init; }
    public string? OutputPath { get; init; }
    public bool SplitFiles { get; init; }
}
