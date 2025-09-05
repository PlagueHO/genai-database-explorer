using System.Threading;
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
    private CancellationToken _lazyLoadingCancellationToken = CancellationToken.None;

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
    /// Loads an entity from a JSON string. Supports envelope { data, embedding } by unwrapping the data
    /// and dispatching to the appropriate entity loader.
    /// </summary>
    private static async Task LoadEntityFromJsonAsync(SemanticModelEntity entity, string rawJson)
    {
        string contentJson = rawJson;
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase))
                    {
                        contentJson = prop.Value.GetRawText();
                        break;
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, fall back to raw JSON
        }

        var fileName = $"{entity.Schema}.{entity.Name}.json";
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var tempFile = Path.Combine(tempDir, fileName);
            await File.WriteAllTextAsync(tempFile, contentJson, Encoding.UTF8);
            var tempDirInfo = new DirectoryInfo(tempDir);
            // Dispatch to derived type loader to avoid base abstract deserialization
            switch (entity)
            {
                case SemanticModelTable t:
                    await t.LoadModelAsync(tempDirInfo);
                    break;
                case SemanticModelView v:
                    await v.LoadModelAsync(tempDirInfo);
                    break;
                case SemanticModelStoredProcedure sp:
                    await sp.LoadModelAsync(tempDirInfo);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported entity type: {entity.GetType().Name}");
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            catch
            {
                // best-effort cleanup
            }
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

        var tables = await GetTablesAsync();
        foreach (var table in tables)
        {
            await table.SaveModelAsync(tablesFolderPath);
        }

        // Save the views to separate files in a subfolder called "views".
        var viewsFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "views"));
        Directory.CreateDirectory(viewsFolderPath.FullName);

        var views = await GetViewsAsync();
        foreach (var view in views)
        {
            await view.SaveModelAsync(viewsFolderPath);
        }

        // Save the stored procedures to separate files in a subfolder called "storedprocedures".
        var storedProceduresFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "storedprocedures"));
        Directory.CreateDirectory(storedProceduresFolderPath.FullName);

        var storedProcedures = await GetStoredProceduresAsync();
        foreach (var storedProcedure in storedProcedures)
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
    /// Finds a table in the semantic model by name and schema asynchronously.
    /// </summary>
    /// <param name="schemaName">The schema name of the table.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The table if found; otherwise, null.</returns>
    public async Task<SemanticModelTable?> FindTableAsync(string schemaName, string tableName)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }
        var tables = await GetTablesAsync();
        return tables.FirstOrDefault(t =>
            string.Equals(t.Schema, schemaName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));
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
        => EnableLazyLoading(modelPath, persistenceStrategy, CancellationToken.None);

    /// <summary>
    /// Enables lazy loading for entity collections using the specified strategy with cancellation support.
    /// </summary>
    /// <param name="modelPath">The path where the model is located.</param>
    /// <param name="persistenceStrategy">The persistence strategy to use for loading entities.</param>
    /// <param name="cancellationToken">Cancellation token used during lazy entity loads.</param>
    public void EnableLazyLoading(DirectoryInfo modelPath, ISemanticModelPersistenceStrategy persistenceStrategy, CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }

        _lazyLoadingCancellationToken = cancellationToken;

        // Capture the current entities before clearing collections
        var capturedTables = Tables.ToList();
        var capturedViews = Views.ToList();
        var capturedStoredProcedures = StoredProcedures.ToList();

        // Create lazy loading proxy for Tables collection (Phase 4a - most commonly accessed)
        _tablesLazyProxy = new LazyLoadingProxy<SemanticModelTable>(
            async () =>
            {
                // Load only the table metadata, then lazy load individual table details
                var tablesFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "tables"));
                var folderExists = Directory.Exists(tablesFolderPath.FullName);

                var tables = new List<SemanticModelTable>();
                foreach (var table in capturedTables)
                {
                    // If the entity file exists, load details; otherwise include captured metadata
                    var fileName = $"{table.Schema}.{table.Name}.json";
                    // Prefer asking the persistence strategy for the entity content (supports remote stores)
                    var relativePath = $"tables/{fileName}";
                    string? entityJson = null;
                    try
                    {
                        entityJson = await persistenceStrategy.LoadEntityContentAsync(modelPath, relativePath, _lazyLoadingCancellationToken);
                    }
                    catch
                    {
                        // best-effort: if persistence strategy cannot provide content, fall through to file check
                        entityJson = null;
                    }

                    if (!string.IsNullOrEmpty(entityJson))
                    {
                        await LoadEntityFromJsonAsync(table, entityJson);
                    }
                    else
                    {
                        var filePath = Path.Combine(tablesFolderPath.FullName, fileName);
                        if (File.Exists(filePath))
                        {
                            await LoadEntityWithEnvelopeSupportAsync(table, tablesFolderPath);
                        }
                    }
                    tables.Add(table);
                }

                // Note: We intentionally do not auto-discover extra table files not referenced by semanticmodel.json.
                // The index is the source of truth across all strategies; discovery could diverge behavior by strategy.
                return tables;
            });

        // Create lazy loading proxy for Views collection (Phase 4d)
        _viewsLazyProxy = new LazyLoadingProxy<SemanticModelView>(
            async () =>
            {
                // Load view metadata and details on demand
                var viewsFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "views"));
                var folderExists = Directory.Exists(viewsFolderPath.FullName);

                // If the directory doesn't exist, return empty collection
                if (!folderExists)
                {
                    return [];
                }

                var views = new List<SemanticModelView>();
                foreach (var view in capturedViews)
                {
                    var fileName = $"{view.Schema}.{view.Name}.json";
                    var relativePath = $"views/{fileName}";
                    string? entityJson = null;
                    try
                    {
                        entityJson = await persistenceStrategy.LoadEntityContentAsync(modelPath, relativePath, _lazyLoadingCancellationToken);
                    }
                    catch
                    {
                        entityJson = null;
                    }

                    if (!string.IsNullOrEmpty(entityJson))
                    {
                        await LoadEntityFromJsonAsync(view, entityJson);
                    }
                    else
                    {
                        var filePath = Path.Combine(viewsFolderPath.FullName, fileName);
                        if (File.Exists(filePath))
                        {
                            await LoadEntityWithEnvelopeSupportAsync(view, viewsFolderPath);
                        }
                    }
                    views.Add(view);
                }

                // Note: We intentionally do not auto-discover extra view files not referenced by semanticmodel.json.
                // The index is the source of truth across all strategies; discovery could diverge behavior by strategy.
                return views;
            });

        // Create lazy loading proxy for StoredProcedures collection (Phase 4d)
        _storedProceduresLazyProxy = new LazyLoadingProxy<SemanticModelStoredProcedure>(
            async () =>
            {
                // Load stored procedure metadata and details on demand
                var storedProceduresFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "storedprocedures"));
                var folderExists = Directory.Exists(storedProceduresFolderPath.FullName);

                // If the directory doesn't exist, return empty collection
                if (!folderExists)
                {
                    return [];
                }

                var storedProcedures = new List<SemanticModelStoredProcedure>();
                foreach (var storedProcedure in capturedStoredProcedures)
                {
                    var fileName = $"{storedProcedure.Schema}.{storedProcedure.Name}.json";
                    var relativePath = $"storedprocedures/{fileName}";
                    string? entityJson = null;
                    try
                    {
                        entityJson = await persistenceStrategy.LoadEntityContentAsync(modelPath, relativePath, _lazyLoadingCancellationToken);
                    }
                    catch
                    {
                        entityJson = null;
                    }

                    if (!string.IsNullOrEmpty(entityJson))
                    {
                        await LoadEntityFromJsonAsync(storedProcedure, entityJson);
                    }
                    else
                    {
                        var filePath = Path.Combine(storedProceduresFolderPath.FullName, fileName);
                        if (File.Exists(filePath))
                        {
                            await LoadEntityWithEnvelopeSupportAsync(storedProcedure, storedProceduresFolderPath);
                        }
                    }
                    storedProcedures.Add(storedProcedure);
                }

                // Note: We intentionally do not auto-discover extra stored procedure files not referenced by semanticmodel.json.
                // The index is the source of truth across all strategies; discovery could diverge behavior by strategy.
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
    /// Finds a view in the semantic model by name and schema asynchronously.
    /// </summary>
    /// <param name="schemaName">The schema name of the view.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The view if found; otherwise, null.</returns>
    public async Task<SemanticModelView?> FindViewAsync(string schemaName, string viewName)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }
        var views = await GetViewsAsync();
        return views.FirstOrDefault(v =>
            string.Equals(v.Schema, schemaName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(v.Name, viewName, StringComparison.OrdinalIgnoreCase));
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
    /// Finds a stored procedure in the semantic model by name and schema asynchronously.
    /// </summary>
    /// <param name="schemaName">The schema name of the stored procedure.</param>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>The stored procedure if found; otherwise, null.</returns>
    public async Task<SemanticModelStoredProcedure?> FindStoredProcedureAsync(string schemaName, string storedProcedureName)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SemanticModel));
        }
        var storedProcedures = await GetStoredProceduresAsync();
        return storedProcedures.FirstOrDefault(sp =>
            string.Equals(sp.Schema, schemaName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(sp.Name, storedProcedureName, StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    /// Loads an entity from disk, supporting files wrapped in an envelope { data, embedding }.
    /// The entity JSON is unwrapped (case-insensitive "data") before deserialization via the existing LoadModelAsync.
    /// </summary>
    private static async Task LoadEntityWithEnvelopeSupportAsync(SemanticModelEntity entity, DirectoryInfo folderPath)
    {
        var fileName = $"{entity.Schema}.{entity.Name}.json";
        var filePath = Path.Combine(folderPath.FullName, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The specified entity file does not exist.", filePath);
        }

        var rawJson = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

        string contentJson = rawJson;
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase))
                    {
                        contentJson = prop.Value.GetRawText();
                        break;
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, fall back to raw JSON
        }

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var tempFile = Path.Combine(tempDir, fileName);
            await File.WriteAllTextAsync(tempFile, contentJson, Encoding.UTF8);
            var tempDirInfo = new DirectoryInfo(tempDir);
            // Dispatch to derived type loader to avoid base abstract deserialization
            switch (entity)
            {
                case SemanticModelTable t:
                    await t.LoadModelAsync(tempDirInfo);
                    break;
                case SemanticModelView v:
                    await v.LoadModelAsync(tempDirInfo);
                    break;
                case SemanticModelStoredProcedure sp:
                    await sp.LoadModelAsync(tempDirInfo);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported entity type: {entity.GetType().Name}");
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}