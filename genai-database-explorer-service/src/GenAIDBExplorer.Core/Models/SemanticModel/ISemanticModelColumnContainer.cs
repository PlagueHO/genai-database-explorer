namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents an semantic model entity that contains columns.
/// </summary>
public interface ISemanticModelColumnContainer
{
    List<SemanticModelColumn> Columns { get; set; }
    void AddColumn(SemanticModelColumn column);
    bool RemoveColumn(SemanticModelColumn column);
}
