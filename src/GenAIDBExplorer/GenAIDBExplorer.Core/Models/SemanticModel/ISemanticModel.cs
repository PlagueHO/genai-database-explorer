using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Repository;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a semantic model.
/// </summary>
public interface ISemanticModel
{
    string Name { get; set; }
    string Source { get; set; }
    string? Description { get; set; }
    Task SaveModelAsync(DirectoryInfo modelPath);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath);
    List<SemanticModelTable> Tables { get; set; }
    void AddTable(SemanticModelTable table);
    bool RemoveTable(SemanticModelTable table);
    SemanticModelTable? FindTable(string schemaName, string tableName);
    List<SemanticModelTable> SelectTables(TableList tableList);
    List<SemanticModelView> Views { get; set; }
    void AddView(SemanticModelView view);
    bool RemoveView(SemanticModelView view);
    SemanticModelView? FindView(string schemaName, string viewName);
    List<SemanticModelStoredProcedure> StoredProcedures { get; set; }
    void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    SemanticModelStoredProcedure? FindStoredProcedure(string schemaName, string storedProcedureName);
    
    /// <summary>
    /// Gets a value indicating whether lazy loading is enabled for this semantic model.
    /// </summary>
    bool IsLazyLoadingEnabled { get; }
    
    /// <summary>
    /// Enables lazy loading for entity collections using the specified strategy.
    /// </summary>
    /// <param name="modelPath">The path where the model is located.</param>
    /// <param name="persistenceStrategy">The persistence strategy to use for loading entities.</param>
    void EnableLazyLoading(DirectoryInfo modelPath, ISemanticModelPersistenceStrategy persistenceStrategy);
    
    /// <summary>
    /// Gets the tables collection with lazy loading support.
    /// </summary>
    /// <returns>A task that resolves to the tables collection.</returns>
    Task<IEnumerable<SemanticModelTable>> GetTablesAsync();
    
    /// <summary>
    /// Accepts a visitor to traverse the semantic model.
    /// </summary>
    /// <param name="visitor">The visitor that will be used to traverse the model.</param>
    void Accept(ISemanticModelVisitor visitor);
}
