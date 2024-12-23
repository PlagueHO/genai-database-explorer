namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents an semantic model entity that contains indexes.
/// </summary>
public interface ISemanticModelIndexContainer
{
    List<SemanticModelIndex> Indexes { get; set; }
    void AddIndex(SemanticModelIndex index);
    bool RemoveIndex(SemanticModelIndex index);
}
