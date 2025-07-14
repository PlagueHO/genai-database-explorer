using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;
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
    Task<SemanticModelTable?> FindTableAsync(string schemaName, string tableName);
    List<SemanticModelTable> SelectTables(TableList tableList);
    List<SemanticModelView> Views { get; set; }
    void AddView(SemanticModelView view);
    bool RemoveView(SemanticModelView view);
    Task<SemanticModelView?> FindViewAsync(string schemaName, string viewName);
    List<SemanticModelStoredProcedure> StoredProcedures { get; set; }
    void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    Task<SemanticModelStoredProcedure?> FindStoredProcedureAsync(string schemaName, string storedProcedureName);

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
    /// Gets the views collection with lazy loading support.
    /// </summary>
    /// <returns>A task that resolves to the views collection.</returns>
    Task<IEnumerable<SemanticModelView>> GetViewsAsync();

    /// <summary>
    /// Gets the stored procedures collection with lazy loading support.
    /// </summary>
    /// <returns>A task that resolves to the stored procedures collection.</returns>
    Task<IEnumerable<SemanticModelStoredProcedure>> GetStoredProceduresAsync();

    /// <summary>
    /// Gets a value indicating whether change tracking is enabled for this semantic model.
    /// </summary>
    bool IsChangeTrackingEnabled { get; }

    /// <summary>
    /// Enables change tracking for this semantic model.
    /// </summary>
    /// <param name="changeTracker">The change tracker to use for tracking entity modifications.</param>
    void EnableChangeTracking(IChangeTracker changeTracker);

    /// <summary>
    /// Gets the change tracker for this semantic model if change tracking is enabled.
    /// </summary>
    IChangeTracker? ChangeTracker { get; }

    /// <summary>
    /// Determines whether there are any unsaved changes in the semantic model.
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// Accepts all changes and marks all entities as clean.
    /// </summary>
    void AcceptAllChanges();

    /// <summary>
    /// Accepts a visitor to traverse the semantic model.
    /// </summary>
    /// <param name="visitor">The visitor that will be used to traverse the model.</param>
    void Accept(ISemanticModelVisitor visitor);
}
