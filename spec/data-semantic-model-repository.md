---
title: Data Semantic Model Repository Pattern Specification
version: 1.1
date_created: 2025-06-22
last_updated: 2025-06-28
owner: GenAI Database Explorer Team
tags: [data, repository, persistence, semantic-model, generative-ai]
---

Repository pattern implementation for persisting AI-consumable semantic models extracted from database schemas.

## 1. Purpose & Scope

**Purpose**: Implement repository pattern for persisting semantic models extracted from database schemas. Provides abstraction for data access, supports three specific persistence strategies: Local Disk JSON files, Azure Storage Blob Storage JSON files, and Cosmos DB Documents.

**Scope**: Database schema extraction, semantic model persistence, lazy loading, change tracking, concurrent operations across Local Disk, Azure Blob Storage, and Cosmos DB storage providers.

**Audience**: Software developers, architects, AI engineers.

**Assumptions**: .NET 9, dependency injection, async patterns, JSON serialization.

## 2. Definitions

- **Repository Pattern**: Encapsulates data access logic with uniform interface
- **Semantic Model**: AI-consumable database schema representation with metadata and relationships
- **Schema Repository**: Extracts/transforms raw database schema information
- **Lazy Loading**: Defers data loading until needed, reduces memory usage
- **Dirty Tracking**: Monitors object changes for selective persistence
- **Unit of Work**: Manages related operations as single transaction

## 3. Requirements, Constraints & Guidelines

### Core Requirements

- **REQ-001**: Repository pattern abstraction for semantic model persistence
- **REQ-002**: Async operations for all I/O activities  
- **REQ-003**: Support three specific persistence strategies: Local Disk JSON files, Azure Storage Blob Storage JSON files, and Cosmos DB Documents
- **REQ-004**: Hierarchical structure with separate entity files
- **REQ-005**: CRUD operations for semantic models
- **REQ-006**: Dependency injection integration
- **REQ-007**: Error handling and logging
- **REQ-008**: Lazy loading for memory optimization
- **REQ-009**: Dirty tracking for selective persistence

### Security Requirements

- **SEC-001**: Path validation prevents directory traversal
- **SEC-002**: Entity name sanitization for file paths
- **SEC-003**: Authentication for persistence operations
- **SEC-004**: Secure handling of connection strings
- **SEC-005**: JSON serialization injection protection

### Performance Requirements

- **PER-001**: Concurrent operations without corruption
- **PER-002**: Entity loading ≤5s for 1000 entities
- **PER-003**: Efficient caching mechanisms
- **PER-004**: Parallel processing for bulk operations
- **PER-005**: Memory optimization via lazy loading

### Constraints

- **CON-001**: .NET 9 compatibility
- **CON-002**: UTF-8 encoding for file operations
- **CON-003**: Human-readable JSON formatting
- **CON-004**: Backward compatibility
- **CON-005**: Entity names ≤128 characters

### Guidelines

- **GUD-001**: Modern C# features (primary constructors, nullable types)
- **GUD-002**: SOLID principles
- **GUD-003**: Structured logging
- **GUD-004**: Consistent async/await patterns
- **GUD-005**: Repository pattern separation of concerns

## 4. Interfaces & Data Contracts

### Core Interfaces

```csharp
/// <summary>Schema repository for database extraction and transformation.</summary>
public interface ISchemaRepository
{
    Task<Dictionary<string, TableInfo>> GetTablesAsync(string? schema = null);
    Task<Dictionary<string, ViewInfo>> GetViewsAsync(string? schema = null);
    Task<Dictionary<string, StoredProcedureInfo>> GetStoredProceduresAsync(string? schema = null);
    Task<string> GetViewDefinitionAsync(ViewInfo view);
    Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table);
    Task<List<Dictionary<string, object>>> GetSampleTableDataAsync(TableInfo tableInfo, int numberOfRecords = 5, bool selectRandom = false);
    Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table);
    Task<SemanticModelView> CreateSemanticModelViewAsync(ViewInfo view);
    Task<SemanticModelStoredProcedure> CreateSemanticModelStoredProcedureAsync(StoredProcedureInfo storedProcedure);
}

/// <summary>Semantic model provider for orchestrating model operations.</summary>
public interface ISemanticModelProvider
{
    SemanticModel CreateSemanticModel();
    Task<SemanticModel> LoadSemanticModelAsync(DirectoryInfo modelPath);
    Task<SemanticModel> ExtractSemanticModelAsync();
}

/// <summary>Persistence strategy interface for different storage providers.</summary>
public interface ISemanticModelPersistenceStrategy
{
    Task SaveModelAsync(SemanticModel model, string containerPath);
    Task<SemanticModel> LoadModelAsync(string containerPath);
    Task DeleteModelAsync(string containerPath);
    Task<bool> ExistsAsync(string containerPath);
    Task<IEnumerable<string>> ListModelsAsync(string basePath);
}

/// <summary>Local disk JSON file persistence strategy.</summary>
public interface ILocalDiskPersistenceStrategy : ISemanticModelPersistenceStrategy
{
    Task SaveModelAsync(SemanticModel model, DirectoryInfo modelPath);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath);
}

/// <summary>Azure Blob Storage JSON file persistence strategy.</summary>
public interface IAzureBlobPersistenceStrategy : ISemanticModelPersistenceStrategy
{
    Task SaveModelAsync(SemanticModel model, string containerName, string blobPrefix);
    Task<SemanticModel> LoadModelAsync(string containerName, string blobPrefix);
    string ConnectionString { get; set; }
}

/// <summary>Cosmos DB document persistence strategy.</summary>
public interface ICosmosPersistenceStrategy : ISemanticModelPersistenceStrategy
{
    Task SaveModelAsync(SemanticModel model, string databaseName, string containerName);
    Task<SemanticModel> LoadModelAsync(string databaseName, string containerName);
    string ConnectionString { get; set; }
    string PartitionKeyPath { get; set; } // Should be "/partitionKey" for hierarchical keys
}

/// <summary>Semantic model with persistence and entity management.</summary>
public interface ISemanticModel
{
    string Name { get; set; }
    string Source { get; set; }
    string? Description { get; set; }
    List<SemanticModelTable> Tables { get; set; }
    List<SemanticModelView> Views { get; set; }
    List<SemanticModelStoredProcedure> StoredProcedures { get; set; }
    Task SaveModelAsync(DirectoryInfo modelPath);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath);
    void AddTable(SemanticModelTable table);
    bool RemoveTable(SemanticModelTable table);
    SemanticModelTable? FindTable(string schemaName, string tableName);
    void Accept(ISemanticModelVisitor visitor);
}

/// <summary>Semantic model entity with persistence capabilities.</summary>
public interface ISemanticModelEntity
{
    string Schema { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
    string? SemanticDescription { get; set; }
    DateTime? SemanticDescriptionLastUpdate { get; set; }
    bool NotUsed { get; set; }
    string? NotUsedReason { get; set; }
    Task SaveModelAsync(DirectoryInfo folderPath);
    Task LoadModelAsync(DirectoryInfo folderPath);
    void Accept(ISemanticModelVisitor visitor);
}
```

### Persistent Storage Structure

The repository supports three persistence strategies, each implementing a hierarchical structure with an index document/file linking to entity documents/files:

#### Local Disk JSON Files Structure

```text
{model-name}/
├── semanticmodel.json           # Index document with model metadata and entity references
├── tables/
│   ├── {schema}.{table-name}.json
│   └── ...
├── views/
│   ├── {schema}.{view-name}.json
│   └── ...
└── storedprocedures/
    ├── {schema}.{procedure-name}.json
    └── ...
```

#### Azure Blob Storage Structure

```text
Container: {container-name}
├── {model-name}/semanticmodel.json     # Index blob with model metadata
├── {model-name}/tables/{schema}.{table-name}.json
├── {model-name}/views/{schema}.{view-name}.json
└── {model-name}/storedprocedures/{schema}.{procedure-name}.json
```

#### Cosmos DB Documents Structure

```text
Database: {database-name}
Container: {container-name}
Documents (each with hierarchical partition key):
├── Document: SemanticModel Index
│   ├── id: "{model-name}"
│   ├── partitionKey: "{model-name}/semanticmodel/index"
│   ├── type: "SemanticModel"
│   └── [model metadata and entity references]
├── Document: Table Entity
│   ├── id: "{model-name}-table-{schema}-{table-name}"
│   ├── partitionKey: "{model-name}/table/{schema}.{table-name}"
│   ├── type: "Table"
│   └── [table definition and columns]
├── Document: View Entity
│   ├── id: "{model-name}-view-{schema}-{view-name}"
│   ├── partitionKey: "{model-name}/view/{schema}.{view-name}"
│   ├── type: "View"
│   └── [view definition and columns]
└── Document: Stored Procedure Entity
    ├── id: "{model-name}-sp-{schema}-{procedure-name}"
    ├── partitionKey: "{model-name}/storedprocedure/{schema}.{procedure-name}"
    ├── type: "StoredProcedure"
    └── [procedure definition and parameters]
```

#### Index Document Schema

**Local Disk & Azure Blob Storage:**

```json
{
  "id": "model-name",
  "type": "SemanticModel",
  "name": "Database Schema Model",
  "source": "SQL Server Adventure Works",
  "description": "Complete schema model for Adventure Works database",
  "tables": [
    {
      "schema": "Sales",
      "name": "Customer",
      "id": "model-name-table-Sales-Customer",
      "relativePath": "tables/Sales.Customer.json"
    }
  ],
  "views": [
    {
      "schema": "Sales",
      "name": "vCustomer",
      "id": "model-name-view-Sales-vCustomer",
      "relativePath": "views/Sales.vCustomer.json"
    }
  ],
  "storedProcedures": [
    {
      "schema": "Sales",
      "name": "uspGetCustomer",
      "id": "model-name-sp-Sales-uspGetCustomer",
      "relativePath": "storedprocedures/Sales.uspGetCustomer.json"
    }
  ],
  "createdDate": "2025-06-28T10:30:00Z",
  "lastModified": "2025-06-28T15:45:00Z"
}
```

**Cosmos DB Index Document:**

```json
{
  "id": "adventureworks",
  "partitionKey": "adventureworks/semanticmodel/index",
  "type": "SemanticModel",
  "name": "Adventure Works Database Schema",
  "source": "SQL Server Adventure Works",
  "description": "Complete schema model for Adventure Works database",
  "tables": [
    {
      "schema": "Sales",
      "name": "Customer",
      "partitionKey": "adventureworks/table/Sales.Customer",
      "documentId": "adventureworks-table-Sales-Customer"
    }
  ],
  "views": [
    {
      "schema": "Sales",
      "name": "vCustomer",
      "partitionKey": "adventureworks/view/Sales.vCustomer",
      "documentId": "adventureworks-view-Sales-vCustomer"
    }
  ],
  "storedProcedures": [
    {
      "schema": "Sales",
      "name": "uspGetCustomer",
      "partitionKey": "adventureworks/storedprocedure/Sales.uspGetCustomer",
      "documentId": "adventureworks-sp-Sales-uspGetCustomer"
    }
  ],
  "createdDate": "2025-06-28T10:30:00Z",
  "lastModified": "2025-06-28T15:45:00Z"
}
```

## 5. Acceptance Criteria

- **AC-001**: Given a semantic model, When SaveModelAsync is called with Local Disk strategy, Then model persists to hierarchical file structure with separate entity files and index document
- **AC-002**: Given a semantic model, When SaveModelAsync is called with Azure Blob Storage strategy, Then model persists as JSON blobs with hierarchical naming and index blob
- **AC-003**: Given a semantic model, When SaveModelAsync is called with Cosmos DB strategy, Then model persists as documents with index document linking to entity documents
- **AC-004**: Given an existing model directory/container, When LoadSemanticModelAsync is called, Then model loads with all entities accessible via lazy loading across all persistence strategies
- **AC-005**: Given concurrent operations, When multiple threads access repository, Then no data corruption occurs across all storage strategies
- **AC-006**: Given 1000 entities, When loading entities, Then operations complete within performance thresholds for each storage strategy
- **AC-007**: Given path input, When performing file operations, Then directory traversal attacks are prevented
- **AC-008**: Given modified entities, When dirty tracking enabled, Then only changed entities persist across all storage strategies
- **AC-009**: Given large model, When lazy loading enabled, Then initial memory usage reduces by ≥70% for all storage strategies
- **AC-010**: Given JSON serialization, When processing data, Then injection attacks are prevented across all persistence strategies
- **AC-011**: Given entity names, When creating file/blob/document paths, Then names are sanitized and length ≤128 characters
- **AC-012**: Given repository operations, When using dependency injection, Then components integrate seamlessly with strategy pattern selection

## 6. Test Automation Strategy

**Test Levels**: Unit, Integration, End-to-End

**Frameworks**:

- MSTest for test execution
- FluentAssertions for readable assertions  
- Moq for mocking dependencies

**Test Data Management**:

- In-memory test data creation
- Cleanup after each test execution
- Isolated test environments

**CI/CD Integration**:

- Automated testing in GitHub Actions pipelines
- Test execution on pull requests
- Code coverage reporting

**Coverage Requirements**:

- Minimum 80% code coverage for repository implementations
- 100% coverage for critical persistence operations
- Branch coverage for error handling paths

**Performance Testing**:

- Load testing for concurrent operations
- Memory usage validation for lazy loading
- Latency testing for large model operations

## 7. Rationale & Context

**Repository Pattern Selection**: Provides clean abstraction between domain logic and data access. Enables testability through mocking, flexibility for multiple persistence strategies, and maintainability through separation of concerns.

**Three-Strategy Persistence Design**:

- **Local Disk JSON**: Development scenarios, small deployments, version control integration, human-readable format
- **Azure Blob Storage JSON**: Cloud-native scenarios, scalable storage, cost-effective for large models, geo-replication support
- **Cosmos DB Documents**: Global distribution, low-latency access, automatic indexing, integrated with Azure ecosystem

**Hierarchical Structure with Index**: Separate entity files enable human readability, version control compatibility, lazy loading support, parallel processing capabilities, and efficient partial updates. Index document provides fast model discovery and metadata access.

**JSON Serialization**: Selected for AI compatibility, human readability, language agnostic consumption, and extensive tooling ecosystem.

**Change Tracking**: Essential for performance optimization (selective persistence), conflict resolution, audit trails, and network optimization in distributed scenarios.

## 8. Examples & Edge Cases

### Basic Usage - Local Disk

```csharp
// Create and extract semantic model to local disk
var localStrategy = serviceProvider.GetRequiredService<ILocalDiskPersistenceStrategy>();
var provider = serviceProvider.GetRequiredService<ISemanticModelProvider>();
var model = await provider.ExtractSemanticModelAsync();
await localStrategy.SaveModelAsync(model, new DirectoryInfo(@"C:\Models\Database"));

// Load existing model from local disk
var loadedModel = await localStrategy.LoadModelAsync(new DirectoryInfo(@"C:\Models\Database"));
```

### Azure Blob Storage Usage

```csharp
// Create and save to Azure Blob Storage
var blobStrategy = serviceProvider.GetRequiredService<IAzureBlobPersistenceStrategy>();
blobStrategy.ConnectionString = "DefaultEndpointsProtocol=https;AccountName=...";
var model = await provider.ExtractSemanticModelAsync();
await blobStrategy.SaveModelAsync(model, "semantic-models", "adventureworks");

// Load from Azure Blob Storage
var loadedModel = await blobStrategy.LoadModelAsync("semantic-models", "adventureworks");
```

### Cosmos DB Usage

```csharp
// Create and save to Cosmos DB
var cosmosStrategy = serviceProvider.GetRequiredService<ICosmosPersistenceStrategy>();
cosmosStrategy.ConnectionString = "AccountEndpoint=https://...;AccountKey=...";
cosmosStrategy.PartitionKeyPath = "/partitionKey"; // Hierarchical partition key path
var model = await provider.ExtractSemanticModelAsync();
await cosmosStrategy.SaveModelAsync(model, "SemanticModels", "Models");

// Load from Cosmos DB
var loadedModel = await cosmosStrategy.LoadModelAsync("SemanticModels", "Models");

// Query specific entity by partition key for optimal performance
// Partition key format: "{model-name}/{entity-type}/{entity-name}"
// Example: "adventureworks/table/Sales.Customer"
```

### Repository Pattern

```csharp
// Schema extraction
var schemaRepo = serviceProvider.GetRequiredService<ISchemaRepository>();
var tables = await schemaRepo.GetTablesAsync("Sales");
var semanticTable = await schemaRepo.CreateSemanticModelTableAsync(tables.First().Value);
```

### Error Handling

```csharp
try
{
    var model = await provider.LoadSemanticModelAsync(invalidPath);
}
catch (DirectoryNotFoundException)
{
    model = provider.CreateSemanticModel(); // Fallback
}
catch (JsonException)
{
    model = await provider.ExtractSemanticModelAsync(); // Re-extract
}
```

### Concurrent Operations

```csharp
// Thread-safe repository access
private readonly SemaphoreSlim _semaphore = new(1, 1);

public async Task<SemanticModel> SafeLoadAsync(DirectoryInfo path)
{
    await _semaphore.WaitAsync();
    try
    {
        return await LoadModelAsync(path);
    }
    finally
    {
        _semaphore.Release();
    }
}
```

## 9. Validation Criteria

**Functional**:

- Persist/retrieve semantic models without data loss across all three storage strategies
- Async operations complete within performance thresholds for Local Disk, Azure Blob Storage, and Cosmos DB
- Concurrent access without corruption across all persistence strategies
- Lazy loading reduces memory usage ≥70% for all storage types
- Change tracking identifies modifications with 100% accuracy
- Index document maintains referential integrity to entity documents/files

**Performance**:

- Model extraction for 100 tables ≤30 seconds
- Entity loading ≤500 milliseconds (Local Disk), ≤2 seconds (Azure Blob), ≤1 second (Cosmos DB)
- Memory usage ≤2GB for 10,000 entities across all storage strategies
- Parallel operations achieve ≥80% CPU utilization
- Cosmos DB queries utilize hierarchical partition key for optimal performance and entity isolation
- Azure Blob Storage operations leverage concurrent uploads/downloads

**Security**:

- Path traversal attack prevention
- Entity name sanitization
- JSON deserialization protection
- Access control enforcement

**Integration**:

- Seamless DI container integration
- Structured logging compliance
- Backward compatibility maintenance

## 10. Related Specifications / Further Reading

- [Infrastructure Deployment Bicep AVM Specification](./infrastructure-deployment-bicep-avm.md)
- [Microsoft .NET Application Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Repository Pattern Documentation](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Entity Framework Core Change Tracking](https://docs.microsoft.com/en-us/ef/core/change-tracking/)
- [System.Text.Json Serialization Guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
