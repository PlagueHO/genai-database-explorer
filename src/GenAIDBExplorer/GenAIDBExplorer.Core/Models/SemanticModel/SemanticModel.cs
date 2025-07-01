using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking;
using GenAIDBExplorer.Core.Models.SemanticModel.JsonConverters;
using GenAIDBExplorer.Core.Models.SemanticModel.LazyLoading;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a semantic model for a database.
/// </summary>
public sealed class SemanticModel(
    string name,
    string source,
    string? description = null
    ) : ISemanticModel, IDisposable
{
    private ILazyLoadingProxy<SemanticModelTable>? _tablesLazyProxy;
    private ILazyLoadingProxy<SemanticModelView>? _viewsLazyProxy;
    private ILazyLoadingProxy<SemanticModelStoredProcedure>? _storedProceduresLazyProxy;
    private IChangeTracker? _changeTracker;
    private bool _disposed;

    /// <summary>
    /// Gets the name of the semantic model.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets the source of the semantic model.
    /// </summary>
    public string Source { get; set; } = source;

    /// <summary>
    /// Gets the description of the semantic model.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; } = description;

    /// <summary>
    /// Gets a value indicating whether lazy loading is enabled for this semantic model.
    /// </summary>
    [JsonIgnore]
    public bool IsLazyLoadingEnabled => _tablesLazyProxy != null || _viewsLazyProxy != null || _storedProceduresLazyProxy != null;

    /// <summary>
    /// Gets a value indicating whether change tracking is enabled for this semantic model.
    /// </summary>
    [JsonIgnore]
    public bool IsChangeTrackingEnabled
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SemanticModel));
            }
            return _changeTracker != null;
        }
    }

    /// <summary>
    /// Gets the change tracker for this semantic model if change tracking is enabled.
    /// </summary>
    [JsonIgnore]
    public IChangeTracker? ChangeTracker
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SemanticModel));
            }
            return _changeTracker;
        }
    }

    /// <summary>
    /// Determines whether there are any unsaved changes in the semantic model.
    /// </summary>
    [JsonIgnore]
    public bool HasUnsavedChanges => _changeTracker?.HasChanges ?? false;

    /// <summary>
    /// Saves the semantic model to the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the model will be saved.</param>
    public async Task SaveModelAsync(DirectoryInfo modelPath)
    {
        JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        // Save the semantic model to a JSON file.
        Directory.CreateDirectory(modelPath.FullName);

        // Save the tables to separate files in a subfolder called "tables".
        var tablesFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "tables"));
        Directory.CreateDirectory(tablesFolderPath.FullName);

        foreach (var table in Tables)
        {
            await table.SaveModelAsync(tablesFolderPath);
        }

        // Save the views to separate files in a subfolder called "views".
        var viewsFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "views"));
        Directory.CreateDirectory(viewsFolderPath.FullName);

        foreach (var view in Views)
        {
            await view.SaveModelAsync(viewsFolderPath);
        }

        // Save the stored procedures to separate files in a subfolder called "storedprocedures".
        var storedProceduresFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "storedprocedures"));
        Directory.CreateDirectory(storedProceduresFolderPath.FullName);

        foreach (var storedProcedure in StoredProcedures)
        {
            await storedProcedure.SaveModelAsync(storedProceduresFolderPath);
        }

        // Add custom converters for the tables, views, and stored procedures
        // to only serialize the name, schema and relative path of the entity.
        jsonSerializerOptions.Converters.Add(new SemanticModelTableJsonConverter());
        jsonSerializerOptions.Converters.Add(new SemanticModelViewJsonConverter());
        jsonSerializerOptions.Converters.Add(new SemanticModelStoredProcedureJsonConverter());

        var semanticModelJsonPath = Path.Combine(modelPath.FullName, "semanticmodel.json");
        await File.WriteAllTextAsync(semanticModelJsonPath, JsonSerializer.Serialize(this, jsonSerializerOptions), Encoding.UTF8);
    }

    /// <summary>
    /// Loads the semantic model from the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the model is located.</param>
    /// <returns>The loaded semantic model.</returns>
    public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
    {
        JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        var semanticModelJsonPath = Path.Combine(modelPath.FullName, "semanticmodel.json");
        if (!File.Exists(semanticModelJsonPath))
        {
            throw new FileNotFoundException("The semantic model file was not found.", semanticModelJsonPath);
        }

        await using var stream = File.OpenRead(semanticModelJsonPath);
        var semanticModel = await JsonSerializer.DeserializeAsync<SemanticModel>(stream, jsonSerializerOptions)
               ?? throw new InvalidOperationException("Failed to deserialize the semantic model.");

        // Load the tables listed in the model from the files in the "tables" subfolder.
        var tablesFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "tables"));
        if (Directory.Exists(tablesFolderPath.FullName))
        {
            foreach (var table in semanticModel.Tables)
            {
                await table.LoadModelAsync(tablesFolderPath);
            }
        }

        // Load the views listed in the model from the files in the "views" subfolder.
        var viewsFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "views"));
        if (Directory.Exists(viewsFolderPath.FullName))
        {
            foreach (var view in semanticModel.Views)
            {
                await view.LoadModelAsync(viewsFolderPath);
            }
        }

        // Load the stored procedures listed in the model from the files in the "storedprocedures" subfolder.
        var storedProceduresFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "storedprocedures"));
        if (Directory.Exists(storedProceduresFolderPath.FullName))
        {
            foreach (var storedProcedure in semanticModel.StoredProcedures)
            {
                await storedProcedure.LoadModelAsync(storedProceduresFolderPath);
            }
        }

        return semanticModel;
    }

    /// <summary>
    /// Gets the tables in the semantic model.
    /// </summary>
    public List<SemanticModelTable> Tables { get; set; } = [];

    /// <summary>
    /// Adds a table to the semantic model.
    /// </summary>
    /// <param name="table">The table to add.</param>
    public void AddTable(SemanticModelTable table)
    {
        Tables.Add(table);
        _changeTracker?.MarkAsDirty(table);
    }

    /// <summary>
    /// Removes a table from the semantic model.
    /// </summary>
    /// <param name="table">The table to remove.</param>
    /// <returns>True if the table was removed; otherwise, false.</returns>
    public bool RemoveTable(SemanticModelTable table)
    {
        var removed = Tables.Remove(table);
        if (removed)
        {
            _changeTracker?.MarkAsDirty(table);
        }
        return removed;
    }

    /// <summary>
    /// Finds a table in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the table.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The table if found; otherwise, null.</returns>
    public SemanticModelTable? FindTable(string schemaName, string tableName)
    {
        return Tables.FirstOrDefault(t => t.Schema == schemaName && t.Name == tableName);
    }

    /// <summary>
    /// Selects tables from the semantic model that match the schema and table names in the provided TableList.
    /// </summary>
    /// <param name="tableList">The list of tables to match.</param>
    /// <returns>A list of matching SemanticModelTable objects.</returns>
    public List<SemanticModelTable> SelectTables(TableList tableList)
    {
        var selectedTables = new List<SemanticModelTable>();

        foreach (var tableInfo in tableList.Tables)
        {
            var matchingTable = Tables.FirstOrDefault(t => t.Schema == tableInfo.SchemaName && t.Name == tableInfo.TableName);
            if (matchingTable != null)
            {
                selectedTables.Add(matchingTable);
            }
        }

        return selectedTables;
    }

    /// <summary>
    /// Enables lazy loading for entity collections using the specified strategy.
    /// </summary>
    /// <param name="modelPath">The path where the model is located.</param>
    /// <param name="persistenceStrategy">The persistence strategy to use for loading entities.</param>
    public void EnableLazyLoading(DirectoryInfo modelPath, ISemanticModelPersistenceStrategy persistenceStrategy)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }

        // Create lazy loading proxy for Tables collection (Phase 4a - most commonly accessed)
        _tablesLazyProxy = new LazyLoadingProxy<SemanticModelTable>(
            async () =>
            {
                // Load only the table metadata, then lazy load individual table details
                var tablesFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "tables"));
                if (!Directory.Exists(tablesFolderPath.FullName))
                {
                    return Enumerable.Empty<SemanticModelTable>();
                }

                var tables = new List<SemanticModelTable>();
                foreach (var table in Tables)
                {
                    await table.LoadModelAsync(tablesFolderPath);
                    tables.Add(table);
                }
                return tables;
            });

        // Create lazy loading proxy for Views collection (Phase 4d)
        _viewsLazyProxy = new LazyLoadingProxy<SemanticModelView>(
            async () =>
            {
                // Load view metadata and details on demand
                var viewsFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "views"));
                if (!Directory.Exists(viewsFolderPath.FullName))
                {
                    return Enumerable.Empty<SemanticModelView>();
                }

                var views = new List<SemanticModelView>();
                foreach (var view in Views)
                {
                    await view.LoadModelAsync(viewsFolderPath);
                    views.Add(view);
                }
                return views;
            });

        // Create lazy loading proxy for StoredProcedures collection (Phase 4d)
        _storedProceduresLazyProxy = new LazyLoadingProxy<SemanticModelStoredProcedure>(
            async () =>
            {
                // Load stored procedure metadata and details on demand
                var storedProceduresFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "storedprocedures"));
                if (!Directory.Exists(storedProceduresFolderPath.FullName))
                {
                    return Enumerable.Empty<SemanticModelStoredProcedure>();
                }

                var storedProcedures = new List<SemanticModelStoredProcedure>();
                foreach (var storedProcedure in StoredProcedures)
                {
                    await storedProcedure.LoadModelAsync(storedProceduresFolderPath);
                    storedProcedures.Add(storedProcedure);
                }
                return storedProcedures;
            });

        // Clear the eagerly loaded collections since we're using lazy loading
        Tables.Clear();
        Views.Clear();
        StoredProcedures.Clear();
    }

    /// <summary>
    /// Enables change tracking for this semantic model.
    /// </summary>
    /// <param name="changeTracker">The change tracker to use for tracking entity modifications.</param>
    public void EnableChangeTracking(IChangeTracker changeTracker)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }

        ArgumentNullException.ThrowIfNull(changeTracker);
        _changeTracker = changeTracker;
    }

    /// <summary>
    /// Accepts all changes and marks all entities as clean.
    /// </summary>
    public void AcceptAllChanges()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }

        _changeTracker?.AcceptAllChanges();
    }

    /// <summary>
    /// Gets the tables collection with lazy loading support.
    /// </summary>
    /// <returns>A task that resolves to the tables collection.</returns>
    public async Task<IEnumerable<SemanticModelTable>> GetTablesAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }

        if (_tablesLazyProxy != null)
        {
            return await _tablesLazyProxy.GetEntitiesAsync();
        }

        // Fall back to eager loaded tables if lazy loading is not enabled
        return Tables;
    }

    /// <summary>
    /// Gets the views collection with lazy loading support.
    /// </summary>
    /// <returns>A task that resolves to the views collection.</returns>
    public async Task<IEnumerable<SemanticModelView>> GetViewsAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }

        if (_viewsLazyProxy != null)
        {
            return await _viewsLazyProxy.GetEntitiesAsync();
        }

        // Fall back to eager loaded views if lazy loading is not enabled
        return Views;
    }

    /// <summary>
    /// Gets the stored procedures collection with lazy loading support.
    /// </summary>
    /// <returns>A task that resolves to the stored procedures collection.</returns>
    public async Task<IEnumerable<SemanticModelStoredProcedure>> GetStoredProceduresAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }

        if (_storedProceduresLazyProxy != null)
        {
            return await _storedProceduresLazyProxy.GetEntitiesAsync();
        }

        // Fall back to eager loaded stored procedures if lazy loading is not enabled
        return StoredProcedures;
    }

    /// <summary>
    /// Gets the views in the semantic model.
    /// </summary>
    public List<SemanticModelView> Views { get; set; } = [];

    /// <summary>
    /// Adds a view to the semantic model.
    /// </summary>
    /// <param name="view">The view to add.</param>
    public void AddView(SemanticModelView view)
    {
        Views.Add(view);
        _changeTracker?.MarkAsDirty(view);
    }

    /// <summary>
    /// Removes a view from the semantic model.
    /// </summary>
    /// <param name="view">The view to remove.</param>
    /// <returns>True if the view was removed; otherwise, false.</returns>
    public bool RemoveView(SemanticModelView view)
    {
        var removed = Views.Remove(view);
        if (removed)
        {
            _changeTracker?.MarkAsDirty(view);
        }
        return removed;
    }

    /// <summary>
    /// Finds a view in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the view.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The view if found; otherwise, null.</returns></returns>
    public SemanticModelView? FindView(string schemaName, string viewName)
    {
        return Views.FirstOrDefault(v => v.Schema == schemaName && v.Name == viewName);
    }

    /// <summary>
    /// Gets the stored procedures in the semantic model.
    /// </summary>
    public List<SemanticModelStoredProcedure> StoredProcedures { get; set; } = [];

    /// <summary>
    /// Adds a stored procedure to the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to add.</param>
    public void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        StoredProcedures.Add(storedProcedure);
        _changeTracker?.MarkAsDirty(storedProcedure);
    }

    /// <summary>
    /// Removes a stored procedure from the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to remove.</param>
    /// <returns>True if the stored procedure was removed; otherwise, false.</returns>
    public bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        var removed = StoredProcedures.Remove(storedProcedure);
        if (removed)
        {
            _changeTracker?.MarkAsDirty(storedProcedure);
        }
        return removed;
    }

    /// <summary>
    /// Finds a stored procedure in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the stored procedure.</param>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>The stored procedure if found; otherwise, null.</returns>
    public SemanticModelStoredProcedure? FindStoredProcedure(string schemaName, string storedProcedureName)
    {
        return StoredProcedures.FirstOrDefault(sp => sp.Schema == schemaName && sp.Name == storedProcedureName);
    }

    /// <summary>
    /// Accepts a visitor to traverse the semantic model.
    /// </summary>
    /// <param name="visitor">The visitor that will be used to traverse the model.</param>
    public void Accept(ISemanticModelVisitor visitor)
    {
        visitor.VisitSemanticModel(this);
        foreach (var table in Tables)
        {
            table.Accept(visitor);
        }

        foreach (var view in Views)
        {
            view.Accept(visitor);
        }

        foreach (var storedProcedure in StoredProcedures)
        {
            storedProcedure.Accept(visitor);
        }
    }

    /// <summary>
    /// Disposes the semantic model and releases any resources used by lazy loading proxies and change tracker.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _tablesLazyProxy?.Dispose();
        _tablesLazyProxy = null;

        _viewsLazyProxy?.Dispose();
        _viewsLazyProxy = null;

        _storedProceduresLazyProxy?.Dispose();
        _storedProceduresLazyProxy = null;

        if (_changeTracker is IDisposable disposableTracker)
        {
            disposableTracker.Dispose();
        }
        _changeTracker = null;

        _disposed = true;
    }

}