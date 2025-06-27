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

**Purpose**: Implement repository pattern for persisting semantic models extracted from database schemas. Provides abstraction for data access, supports multiple persistence strategies (file, database, cloud).

**Scope**: Database schema extraction, semantic model persistence, lazy loading, change tracking, concurrent operations.

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
- **REQ-003**: Support file-based and database persistence strategies
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

## 5. Acceptance Criteria

- **AC-001**: Given a semantic model, When SaveModelAsync is called, Then model persists to hierarchical file structure with separate entity files
- **AC-002**: Given an existing model directory, When LoadSemanticModelAsync is called, Then model loads with all entities accessible via lazy loading
- **AC-003**: Given concurrent operations, When multiple threads access repository, Then no data corruption occurs
- **AC-004**: Given 1000 entities, When loading entities, Then operations complete within 5 seconds
- **AC-005**: Given path input, When performing file operations, Then directory traversal attacks are prevented
- **AC-006**: Given modified entities, When dirty tracking enabled, Then only changed entities persist
- **AC-007**: Given large model, When lazy loading enabled, Then initial memory usage reduces by ≥70%
- **AC-008**: Given JSON serialization, When processing data, Then injection attacks are prevented
- **AC-009**: Given entity names, When creating file paths, Then names are sanitized and length ≤128 characters
- **AC-010**: Given repository operations, When using dependency injection, Then components integrate seamlessly

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

**File-Based Persistence**: Hierarchical structure with separate entity files enables human readability, version control compatibility, lazy loading support, and parallel processing capabilities.

**JSON Serialization**: Selected for AI compatibility, human readability, language agnostic consumption, and extensive tooling ecosystem.

**Change Tracking**: Essential for performance optimization (selective persistence), conflict resolution, audit trails, and network optimization in distributed scenarios.

## 8. Examples & Edge Cases

### Basic Usage

```csharp
// Create and extract semantic model
var provider = serviceProvider.GetRequiredService<ISemanticModelProvider>();
var model = await provider.ExtractSemanticModelAsync();
await model.SaveModelAsync(new DirectoryInfo(@"C:\Models\Database"));

// Load existing model
var loadedModel = await provider.LoadSemanticModelAsync(new DirectoryInfo(@"C:\Models\Database"));
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

- Persist/retrieve semantic models without data loss
- Async operations complete within performance thresholds
- Concurrent access without corruption
- Lazy loading reduces memory usage ≥70%
- Change tracking identifies modifications with 100% accuracy

**Performance**:

- Model extraction for 100 tables ≤30 seconds
- Entity loading ≤500 milliseconds
- Memory usage ≤2GB for 10,000 entities
- Parallel operations achieve ≥80% CPU utilization

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
