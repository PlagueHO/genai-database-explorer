---
title: Semantic Model Persistence Repository Pattern Specification
version: 1.0
date_created: 2025-06-22
last_updated: 2025-06-22
owner: GenAI Database Explorer Team
tags: [data, repository, persistence, semantic-model, generative-ai]
---

This specification defines the requirements, constraints, and interfaces for implementing the repository pattern to persist semantic models extracted from database schemas for the GenAI Database Explorer application.

## 1. Purpose & Scope

This specification defines the requirements for implementing a repository pattern to persist and manage semantic models extracted from database schemas. The semantic model represents a rich, AI-consumable representation of database structures including tables, views, stored procedures, and their relationships. The repository pattern provides abstraction for data access operations, enabling multiple persistence strategies and supporting both file-based and future database-based storage mechanisms.

**Intended Audience:** Software developers, architects, and AI engineers working on the GenAI Database Explorer application.

**Assumptions:**

- The application uses .NET 9 with modern C# language features
- Dependency injection is used for managing component dependencies
- Asynchronous programming patterns are preferred for I/O operations
- JSON serialization is the primary persistence format for file-based storage

## 2. Definitions

- **AI**: Artificial Intelligence
- **DI**: Dependency Injection
- **DTO**: Data Transfer Object
- **GenAI**: Generative Artificial Intelligence
- **I/O**: Input/Output operations
- **JSON**: JavaScript Object Notation
- **ORM**: Object-Relational Mapping
- **POCO**: Plain Old CLR Object
- **Repository Pattern**: A design pattern that encapsulates data access logic and provides a uniform interface for accessing domain objects
- **Semantic Model**: A rich representation of database schema that includes metadata, relationships, and AI-friendly descriptions
- **Schema Repository**: A repository specifically responsible for extracting and transforming raw database schema information
- **Semantic Model Provider**: A service that orchestrates the creation, extraction, and management of semantic models
- **Lazy Loading**: A design pattern that defers the loading of data until it is actually needed, reducing initial memory usage
- **Dirty Tracking**: A mechanism that monitors changes to objects and identifies which entities need to be persisted

## 3. Requirements, Constraints & Guidelines

### Core Requirements

- **REQ-001**: The repository pattern MUST provide abstraction for semantic model persistence operations
- **REQ-002**: The implementation MUST support asynchronous operations for all I/O-bound activities
- **REQ-003**: The repository MUST handle both file-based and future database-based persistence strategies
- **REQ-004**: The semantic model MUST be persisted in a hierarchical structure with separate files for entities
- **REQ-005**: The repository MUST support CRUD operations (Create, Read, Update, Delete) for semantic models
- **REQ-006**: The implementation MUST use dependency injection for component management
- **REQ-007**: The repository MUST provide proper error handling and logging capabilities
- **REQ-008**: The repository MUST support lazy loading of semantic model entities to optimize memory usage
- **REQ-009**: The repository MUST implement dirty tracking to identify modified entities for selective persistence
- **REQ-010**: The repository MUST support partial model loading based on entity access patterns

### Security Requirements

- **SEC-001**: All file I/O operations MUST validate input paths to prevent directory traversal attacks
- **SEC-002**: The repository MUST sanitize entity names before using them in file paths
- **SEC-003**: Access to persistence operations MUST be controlled through proper authentication mechanisms
- **SEC-004**: Sensitive connection strings and configuration data MUST be handled securely
- **SEC-005**: All JSON serialization MUST be protected against injection attacks

### Performance Requirements

- **PER-001**: The repository MUST support concurrent operations without data corruption
- **PER-002**: Entity loading operations MUST complete within 5 seconds for models with up to 1000 entities
- **PER-003**: The repository MUST implement efficient caching mechanisms to minimize disk I/O
- **PER-004**: Parallel processing MUST be utilized for bulk operations where applicable
- **PER-005**: Memory usage MUST be optimized through lazy loading and proxy patterns

### Constraints

- **CON-001**: The implementation MUST be compatible with .NET 9 and modern C# language features
- **CON-002**: File-based persistence MUST use UTF-8 encoding for all text operations
- **CON-003**: Entity files MUST be saved with human-readable JSON formatting
- **CON-004**: The repository MUST maintain backward compatibility with existing semantic model formats
- **CON-005**: Maximum entity name length is limited to 128 characters per database restrictions

### Guidelines

- **GUD-001**: Use modern C# language features including primary constructors and nullable reference types
- **GUD-002**: Follow SOLID principles in repository design and implementation
- **GUD-003**: Implement comprehensive logging using structured logging patterns
- **GUD-004**: Use async/await patterns consistently throughout the implementation
- **GUD-005**: Apply the repository pattern to separate persistence concerns from business logic
- **GUD-006**: Implement lazy loading using proxy patterns or virtual properties to minimize memory footprint
- **GUD-007**: Use change tracking mechanisms similar to Entity Framework for dirty detection
- **GUD-008**: Implement smart loading strategies based on entity access patterns and relationships

### Patterns to Follow

- **PAT-001**: Implement the Unit of Work pattern for managing related semantic model operations
- **PAT-002**: Use the Factory pattern for creating repository instances based on persistence strategy
- **PAT-003**: Apply the Strategy pattern for different persistence mechanisms (file, database, cloud)
- **PAT-004**: Use the Builder pattern for complex semantic model construction operations
- **PAT-005**: Implement the Proxy pattern for lazy loading of semantic model entities
- **PAT-006**: Use the Observer pattern for dirty tracking and change notification
- **PAT-007**: Apply the Command pattern for tracking entity modifications and implementing undo operations

## 4. Interfaces & Data Contracts

### Core Repository Interfaces

```csharp
/// <summary>
/// Defines the contract for schema repository operations that extract and transform 
/// raw database schema information into semantic model entities.
/// </summary>
public interface ISchemaRepository
{
    // Schema Discovery Operations
    Task<Dictionary<string, TableInfo>> GetTablesAsync(string? schema = null);
    Task<Dictionary<string, ViewInfo>> GetViewsAsync(string? schema = null);
    Task<Dictionary<string, StoredProcedureInfo>> GetStoredProceduresAsync(string? schema = null);
    
    // Schema Detail Operations
    Task<string> GetViewDefinitionAsync(ViewInfo view);
    Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table);
    Task<List<SemanticModelColumn>> GetColumnsForViewAsync(ViewInfo view);
    Task<List<Dictionary<string, object>>> GetSampleTableDataAsync(TableInfo tableInfo, int numberOfRecords = 5, bool selectRandom = false);
    Task<List<Dictionary<string, object>>> GetSampleViewDataAsync(ViewInfo viewInfo, int numberOfRecords = 5, bool selectRandom = false);
    
    // Semantic Model Factory Operations
    Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table);
    Task<SemanticModelView> CreateSemanticModelViewAsync(ViewInfo view);
    Task<SemanticModelStoredProcedure> CreateSemanticModelStoredProcedureAsync(StoredProcedureInfo storedProcedure);
}

/// <summary>
/// Defines the contract for semantic model provider operations that orchestrate
/// the creation, extraction, and management of complete semantic models.
/// </summary>
public interface ISemanticModelProvider
{
    /// <summary>
    /// Creates a new empty semantic model configured with project information.
    /// </summary>
    SemanticModel CreateSemanticModel();
    
    /// <summary>
    /// Loads an existing semantic model from the specified path.
    /// </summary>
    Task<SemanticModel> LoadSemanticModelAsync(DirectoryInfo modelPath);
    
    /// <summary>
    /// Extracts a complete semantic model from the connected database.
    /// </summary>
    Task<SemanticModel> ExtractSemanticModelAsync();
}

/// <summary>
/// Defines the contract for semantic model persistence operations.
/// </summary>
public interface ISemanticModel
{
    string Name { get; set; }
    string Source { get; set; }
    string? Description { get; set; }
    
    // Entity Collections
    List<SemanticModelTable> Tables { get; set; }
    List<SemanticModelView> Views { get; set; }
    List<SemanticModelStoredProcedure> StoredProcedures { get; set; }
    
    // Persistence Operations
    Task SaveModelAsync(DirectoryInfo modelPath);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath);
    
    // Entity Management Operations
    void AddTable(SemanticModelTable table);
    bool RemoveTable(SemanticModelTable table);
    SemanticModelTable? FindTable(string schemaName, string tableName);
    List<SemanticModelTable> SelectTables(TableList tableList);
    
    void AddView(SemanticModelView view);
    bool RemoveView(SemanticModelView view);
    SemanticModelView? FindView(string schemaName, string viewName);
    
    void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    SemanticModelStoredProcedure? FindStoredProcedure(string schemaName, string procedureName);
      // Visitor Pattern Support
    void Accept(ISemanticModelVisitor visitor);
}

/// <summary>
/// Defines the contract for semantic model entities with persistence capabilities.
/// </summary>
public interface ISemanticModelEntity
{
    string Schema { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
    string? SemanticDescription { get; set; }
    DateTime? SemanticDescriptionLastUpdate { get; set; }
    bool NotUsed { get; set; }
    string? NotUsedReason { get; set; }
    
    // Persistence Operations
    Task SaveModelAsync(DirectoryInfo folderPath);
    Task LoadModelAsync(DirectoryInfo folderPath);
    FileInfo GetModelEntityFilename();
    DirectoryInfo GetModelPath();
    void SetSemanticDescription(string semanticDescription);
    
    // Visitor Pattern Support
    void Accept(ISemanticModelVisitor visitor);
}
```

### Advanced Repository Interfaces

```csharp
/// <summary>
/// Defines the contract for change tracking in semantic model entities.
/// </summary>
public interface ITrackable
{
    bool IsModified { get; }
    bool IsNew { get; }
    bool IsDeleted { get; }
    void MarkAsModified();
    void MarkAsNew();
    void MarkAsDeleted();
    void ResetChangeTracking();
    DateTime LastModified { get; set; }
}

/// <summary>
/// Defines the contract for lazy-loadable semantic model entities.
/// </summary>
public interface ILazyLoadable<T> where T : ISemanticModelEntity
{
    bool IsLoaded { get; }
    Task<T> LoadAsync();
    void Unload();
}

/// <summary>
/// Defines the contract for unit of work pattern implementation.
/// </summary>
public interface ISemanticModelUnitOfWork : IDisposable
{
    ISemanticModelRepository SemanticModels { get; }
    ISemanticEntityRepository<SemanticModelTable> Tables { get; }
    ISemanticEntityRepository<SemanticModelView> Views { get; }
    ISemanticEntityRepository<SemanticModelStoredProcedure> StoredProcedures { get; }
    
    Task<int> SaveChangesAsync();
    Task RollbackAsync();
    void ClearChangeTracking();
}
```

### Persistence Strategy Interfaces

```csharp
/// <summary>
/// Defines the contract for different semantic model persistence strategies.
/// </summary>
public interface ISemanticModelPersistenceStrategy
{
    Task SaveAsync(SemanticModel model, string location);
    Task<SemanticModel> LoadAsync(string location);
    Task<bool> ExistsAsync(string location);
    Task DeleteAsync(string location);
    
    // Lazy Loading Support
    Task<T> LoadEntityAsync<T>(string entityId, string location) where T : ISemanticModelEntity;
    Task<bool> EntityExistsAsync(string entityId, string location);
    
    // Selective Persistence Support
    Task SaveModifiedEntitiesAsync(IEnumerable<ITrackable> modifiedEntities, string location);
    Task SaveEntityAsync<T>(T entity, string location) where T : ISemanticModelEntity;
}

/// <summary>
/// File-based persistence strategy for semantic models with lazy loading support.
/// </summary>
public interface IFileSemanticModelPersistenceStrategy : ISemanticModelPersistenceStrategy
{
    Task SaveAsync(SemanticModel model, DirectoryInfo directory);
    Task<SemanticModel> LoadAsync(DirectoryInfo directory);
    Task<bool> ExistsAsync(DirectoryInfo directory);
    Task DeleteAsync(DirectoryInfo directory);
    
    // Lazy Loading Support
    Task<T> LoadEntityAsync<T>(string entityId, DirectoryInfo directory) where T : ISemanticModelEntity;
    Task<IEnumerable<string>> GetEntityIdsAsync(DirectoryInfo directory, Type entityType);
    
    // Selective Persistence Support
    Task SaveModifiedEntitiesAsync(IEnumerable<ITrackable> modifiedEntities, DirectoryInfo directory);
    Task<FileInfo> GetEntityFilePathAsync(string entityId, DirectoryInfo directory, Type entityType);
}
```

### Data Transfer Objects

```csharp
/// <summary>
/// Represents the hierarchical structure of a persisted semantic model.
/// </summary>
public sealed record SemanticModelStructure
{
    public string Name { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; init; }
    public List<EntityReference> Tables { get; init; } = [];
    public List<EntityReference> Views { get; init; } = [];
    public List<EntityReference> StoredProcedures { get; init; } = [];
}

/// <summary>
/// Represents a reference to a semantic model entity in the persistence layer.
/// </summary>
public sealed record EntityReference
{
    public string Schema { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public DateTime LastModified { get; init; }
    public bool IsLoaded { get; init; }
}

/// <summary>
/// Represents the persistence configuration for semantic models.
/// </summary>
public sealed record SemanticModelPersistenceOptions
{
    public string BasePath { get; init; } = string.Empty;
    public bool EnableLazyLoading { get; init; } = true;
    public bool EnableChangeTracking { get; init; } = true;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public TimeSpan CacheExpiration { get; init; } = TimeSpan.FromMinutes(30);
    public bool UseCompression { get; init; } = false;
    public JsonSerializerOptions JsonOptions { get; init; } = new() { WriteIndented = true };
}
```

## 5. Rationale & Context

### Repository Pattern Selection

The repository pattern was chosen to provide a clean abstraction layer between the domain logic and data access concerns. This pattern enables:

1. **Testability**: Easy mocking of data access operations for unit testing
2. **Flexibility**: Support for multiple persistence strategies (file, database, cloud)
3. **Maintainability**: Clear separation of concerns between business logic and data access
4. **Extensibility**: Easy addition of new persistence mechanisms without affecting existing code

### File-Based Persistence Structure

The hierarchical file structure with separate entity files provides:

1. **Human Readability**: Individual JSON files can be examined and edited manually
2. **Version Control Compatibility**: Changes to individual entities create focused diffs
3. **Lazy Loading Support**: Entities can be loaded on-demand to optimize memory usage
4. **Parallel Processing**: Multiple entities can be processed concurrently

### JSON Serialization Strategy

JSON was selected as the primary serialization format because:

1. **AI Compatibility**: Generative AI models work effectively with JSON structures
2. **Human Readability**: Developers can easily inspect and modify semantic models
3. **Language Agnostic**: JSON can be consumed by various programming languages
4. **Tooling Support**: Extensive ecosystem of JSON processing tools available

### Change Tracking Implementation

Change tracking mechanisms are essential for:

1. **Performance Optimization**: Only modified entities need to be persisted
2. **Conflict Resolution**: Detecting concurrent modifications to prevent data loss
3. **Audit Trail**: Maintaining history of changes for debugging and compliance
4. **Selective Synchronization**: Optimizing network operations in distributed scenarios

## 6. Examples & Edge Cases

### Basic Semantic Model Creation and Persistence

```csharp
// Create a semantic model provider
var semanticModelProvider = serviceProvider.GetRequiredService<ISemanticModelProvider>();

// Extract semantic model from database
var semanticModel = await semanticModelProvider.ExtractSemanticModelAsync();

// Persist the model to file system
var modelPath = new DirectoryInfo(@"C:\SemanticModels\MyDatabase");
await semanticModel.SaveModelAsync(modelPath);

// Load the model back from file system
var loadedModel = await semanticModelProvider.LoadSemanticModelAsync(modelPath);
```

### Repository Usage for Schema Extraction

```csharp
// Get schema repository instance
var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

// Extract tables for specific schema
var tables = await schemaRepository.GetTablesAsync("Sales");

// Create semantic model entities
var semanticTables = new List<SemanticModelTable>();
foreach (var table in tables.Values)
{
    var semanticTable = await schemaRepository.CreateSemanticModelTableAsync(table);
    semanticTables.Add(semanticTable);
}
```

### Lazy Loading Implementation

```csharp
public class LazySemanticModelTable : ILazyLoadable<SemanticModelTable>
{
    private SemanticModelTable? _table;
    private readonly string _entityId;
    private readonly ISemanticModelPersistenceStrategy _persistenceStrategy;
    private readonly DirectoryInfo _modelPath;

    public bool IsLoaded => _table != null;

    public async Task<SemanticModelTable> LoadAsync()
    {
        if (_table == null)
        {
            _table = await _persistenceStrategy.LoadEntityAsync<SemanticModelTable>(_entityId, _modelPath.FullName);
        }
        return _table;
    }

    public void Unload()
    {
        _table = null;
    }
}
```

### Unit of Work Pattern Implementation

```csharp
// Begin unit of work
using var unitOfWork = serviceProvider.GetRequiredService<ISemanticModelUnitOfWork>();

// Modify entities
var table = await unitOfWork.Tables.GetByIdAsync("Sales.Customer");
table.Description = "Updated customer information";
unitOfWork.Tables.Update(table);

// Add new entity
var newView = new SemanticModelView("Sales", "CustomerSummary");
unitOfWork.Views.Add(newView);

// Persist all changes atomically
await unitOfWork.SaveChangesAsync();
```

### Error Handling Edge Cases

```csharp
// Handle missing directory
try
{
    var model = await semanticModelProvider.LoadSemanticModelAsync(
        new DirectoryInfo(@"C:\NonExistent\Path"));
}
catch (DirectoryNotFoundException ex)
{
    logger.LogError(ex, "Semantic model directory not found: {Path}", ex.Message);
    // Fallback to creating new model
    model = semanticModelProvider.CreateSemanticModel();
}

// Handle corrupted model files
try
{
    var model = await semanticModelProvider.LoadSemanticModelAsync(modelPath);
}
catch (JsonException ex)
{
    logger.LogError(ex, "Corrupted semantic model JSON: {Path}", modelPath);
    // Fallback to re-extracting from database
    model = await semanticModelProvider.ExtractSemanticModelAsync();
}
```

### Concurrent Access Handling

```csharp
public class ThreadSafeSemanticModelRepository
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, DateTime> _lastModified = new();

    public async Task<SemanticModel> LoadWithConcurrencyCheckAsync(DirectoryInfo modelPath)
    {
        await _semaphore.WaitAsync();
        try
        {
            var currentModified = Directory.GetLastWriteTime(modelPath.FullName);
            var key = modelPath.FullName;
            
            if (_lastModified.TryGetValue(key, out var lastKnown) && currentModified > lastKnown)
            {
                throw new InvalidOperationException("Model has been modified by another process");
            }
            
            var model = await LoadModelInternalAsync(modelPath);
            _lastModified[key] = currentModified;
            return model;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### Performance Optimization Patterns

```csharp
// Batch loading with parallel processing
public async Task<IEnumerable<SemanticModelTable>> LoadTablesAsync(
    IEnumerable<string> tableIds, 
    DirectoryInfo modelPath)
{
    var parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    var tables = new ConcurrentBag<SemanticModelTable>();
    
    await Parallel.ForEachAsync(tableIds, parallelOptions, async (tableId, ct) =>
    {
        var table = await LoadEntityAsync<SemanticModelTable>(tableId, modelPath.FullName);
        tables.Add(table);
    });

    return tables;
}

// Memory-efficient streaming for large models
public async IAsyncEnumerable<SemanticModelTable> StreamTablesAsync(DirectoryInfo modelPath)
{
    var tableDirectory = new DirectoryInfo(Path.Combine(modelPath.FullName, "tables"));
    var tableFiles = tableDirectory.GetFiles("*.json");

    foreach (var file in tableFiles)
    {
        using var stream = file.OpenRead();
        var table = await JsonSerializer.DeserializeAsync<SemanticModelTable>(stream);
        if (table != null)
        {
            yield return table;
        }
    }
}
```

## 7. Validation Criteria

### Functional Validation

- **VAL-001**: Repository MUST successfully persist and retrieve semantic models without data loss
- **VAL-002**: All async operations MUST complete within specified performance thresholds
- **VAL-003**: Concurrent access scenarios MUST not result in data corruption
- **VAL-004**: Lazy loading MUST reduce initial memory footprint by at least 70%
- **VAL-005**: Change tracking MUST accurately identify modified entities with 100% precision

### Performance Validation

- **VAL-006**: Model extraction for 100 tables MUST complete within 30 seconds
- **VAL-007**: Individual entity loading MUST complete within 500 milliseconds
- **VAL-008**: Memory usage MUST not exceed 2GB for models with 10,000 entities
- **VAL-009**: Parallel operations MUST achieve at least 80% CPU utilization
- **VAL-010**: File I/O operations MUST be optimized to minimize disk seeks

### Security Validation

- **VAL-011**: Path traversal attacks MUST be prevented through input validation
- **VAL-012**: Entity name sanitization MUST prevent file system injection
- **VAL-013**: JSON deserialization MUST be protected against malicious payloads
- **VAL-014**: Access control mechanisms MUST prevent unauthorized modifications
- **VAL-015**: Sensitive data MUST not be logged or exposed in error messages

### Integration Validation

- **VAL-016**: Repository MUST integrate seamlessly with dependency injection container
- **VAL-017**: Error handling MUST provide meaningful diagnostics for troubleshooting
- **VAL-018**: Logging output MUST follow structured logging patterns
- **VAL-019**: Configuration changes MUST not require application restart
- **VAL-020**: Backward compatibility MUST be maintained for existing semantic models

## 8. Related Specifications / Further Reading

- [Infrastructure Deployment Bicep AVM Specification](./infrastructure-deployment-bicep-avm.md)
- [Microsoft .NET Application Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Repository Pattern Documentation](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Azure Well-Architected Framework](https://docs.microsoft.com/en-us/azure/architecture/framework/)
- [Entity Framework Core Change Tracking](https://docs.microsoft.com/en-us/ef/core/change-tracking/)
- [System.Text.Json Serialization Guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
- [Async Programming Best Practices](https://docs.microsoft.com/en-us/dotnet/csharp/async)
- [SOLID Principles in C#](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
