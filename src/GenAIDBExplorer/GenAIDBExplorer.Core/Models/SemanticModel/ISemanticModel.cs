namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a semantic model.
/// </summary>
public interface ISemanticModel
{
    string Name { get; set; }
    string Source { get; set; }
    string? Description { get; set; }
    List<SemanticModelTable> Tables { get; set; }
    List<SemanticModelView> Views { get; set; }
    List<SemanticModelStoredProcedure> StoredProcedures { get; set; }
    void AddTable(SemanticModelTable table);
    bool RemoveTable(SemanticModelTable table);
    SemanticModelTable? FindTable(string schemaName, string tableName);
    void AddView(SemanticModelView view);
    bool RemoveView(SemanticModelView view);
    SemanticModelView? FindView(string schemaName, string viewName);
    void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    SemanticModelStoredProcedure? FindStoredProcedure(string schemaName, string storedProcedureName);
    Task SaveModelAsync(DirectoryInfo modelPath, bool splitModel = false);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath);
}
