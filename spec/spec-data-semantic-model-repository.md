---
title: Data Semantic Model Repository Pattern Specification
version: 1.2
date_created: 2025-06-22
last_updated: 2025-07-02
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
- **REQ-010**: Builder pattern for repository options configuration
- **REQ-011**: Fluent interface for repository options construction
- **REQ-012**: Immutable options objects after construction
- **REQ-013**: Performance monitoring implementation that builds on and aligns with the core monitoring requirements defined in the [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md), with graceful degradation when telemetry infrastructure is unavailable
- **REQ-014**: Zero external dependencies and full functionality when OpenTelemetry services are not configured or available
- **REQ-015**: Compatibility with .NET Aspire telemetry configuration through standard OpenTelemetry environment variables and OTLP endpoints
- **REQ-016**: Async Find methods support both lazy-loaded and eager-loaded scenarios transparently
- **REQ-017**: Automatic loading strategy detection routes Find methods to appropriate collection access
- **REQ-018**: Breaking API changes for Find methods to support lazy loading without consumer knowledge
- **REQ-019**: Migration path documentation for existing Find method consumers

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
- **PER-004**: Memory optimization via lazy loading
- **PER-005**: Performance monitoring that extends the core requirements defined in the [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md) with repository-specific metrics and operations, ensuring zero performance degradation when telemetry services are unavailable

### Constraints

- **CON-001**: .NET 9 compatibility
- **CON-002**: UTF-8 encoding for file operations
- **CON-003**: Human-readable JSON formatting
- **CON-004**: Data storage format backward compatibility (existing semantic model files must remain loadable)
- **CON-005**: Entity names ≤128 characters

### Guidelines

- **GUD-001**: Modern C# features (primary constructors, nullable types)
- **GUD-002**: SOLID principles
- **GUD-003**: Structured logging
- **GUD-004**: Consistent async/await patterns
- **GUD-005**: Repository pattern separation of concerns
- **GUD-006**: Builder pattern for complex configuration scenarios
- **GUD-007**: Immutable options objects to prevent unintended modifications
- **GUD-008**: Fluent interface design for improved readability
- **GUD-009**: Method chaining for builder pattern implementation

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

/// <summary>Repository for semantic model persistence with flexible loading options.</summary>
public interface ISemanticModelRepository
{
    Task SaveModelAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null);
    Task SaveChangesAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, string? strategyName = null);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, SemanticModelRepositoryOptions options);
}

/// <summary>Immutable options record for configuring semantic model repository operations.</summary>
public record SemanticModelRepositoryOptions
{
    public bool EnableLazyLoading { get; init; } = false;
    public bool EnableChangeTracking { get; init; } = false;
    public bool EnableCaching { get; init; } = false;
    public string? StrategyName { get; init; }
    public TimeSpan? CacheExpiration { get; init; }
    public int? MaxConcurrentOperations { get; init; }
    public PerformanceMonitoringOptions? PerformanceMonitoring { get; init; }
}

/// <summary>Immutable configuration record for performance monitoring and telemetry.</summary>
public record PerformanceMonitoringOptions
{
    public bool EnableLocalMonitoring { get; init; } = true;
    public TimeSpan? MetricsRetentionPeriod { get; init; } = TimeSpan.FromHours(24);
}

/// <summary>Builder for creating SemanticModelRepositoryOptions with fluent interface.</summary>
public interface ISemanticModelRepositoryOptionsBuilder
{
    ISemanticModelRepositoryOptionsBuilder WithLazyLoading(bool enabled = true);
    ISemanticModelRepositoryOptionsBuilder WithChangeTracking(bool enabled = true);
    ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled = true);
    ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled, TimeSpan expiration);
    ISemanticModelRepositoryOptionsBuilder WithStrategyName(string strategyName);
    ISemanticModelRepositoryOptionsBuilder WithMaxConcurrentOperations(int maxOperations);
    ISemanticModelRepositoryOptionsBuilder WithPerformanceMonitoring(Action<IPerformanceMonitoringOptionsBuilder> configure);
    SemanticModelRepositoryOptions Build();
}

/// <summary>Builder for performance monitoring configuration.</summary>
public interface IPerformanceMonitoringOptionsBuilder
{
    IPerformanceMonitoringOptionsBuilder EnableLocalMonitoring(bool enabled = true);
    IPerformanceMonitoringOptionsBuilder WithMetricsRetention(TimeSpan retention);
    PerformanceMonitoringOptions Build();
}

/// <summary>Thread-safe immutable builder implementation for SemanticModelRepositoryOptions.</summary>
public class SemanticModelRepositoryOptionsBuilder : ISemanticModelRepositoryOptionsBuilder
{
    private readonly SemanticModelRepositoryOptions _current;

    // Private constructor - only used internally for immutable chaining
    private SemanticModelRepositoryOptionsBuilder(SemanticModelRepositoryOptions options)
    {
        _current = options;
    }

    // Public factory method
    public static ISemanticModelRepositoryOptionsBuilder Create()
    {
        return new SemanticModelRepositoryOptionsBuilder(new SemanticModelRepositoryOptions());
    }

    public ISemanticModelRepositoryOptionsBuilder WithLazyLoading(bool enabled = true)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { EnableLazyLoading = enabled });
    }

    public ISemanticModelRepositoryOptionsBuilder WithChangeTracking(bool enabled = true)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { EnableChangeTracking = enabled });
    }

    public ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled = true)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { EnableCaching = enabled });
    }

    public ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled, TimeSpan expiration)
    {
        // Create new instance with multiple properties (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with 
        { 
            EnableCaching = enabled,
            CacheExpiration = expiration
        });
    }

    public ISemanticModelRepositoryOptionsBuilder WithStrategyName(string strategyName)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { StrategyName = strategyName });
    }

    public ISemanticModelRepositoryOptionsBuilder WithMaxConcurrentOperations(int maxOperations)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { MaxConcurrentOperations = maxOperations });
    }

    public ISemanticModelRepositoryOptionsBuilder WithPerformanceMonitoring(Action<IPerformanceMonitoringOptionsBuilder> configure)
    {
        var builder = PerformanceMonitoringOptionsBuilder.Create();
        configure(builder);
        var performanceOptions = builder.Build();
        
        // Create new instance instead of mutating current state (immutable pattern)
        return new SemanticModelRepositoryOptionsBuilder(_current with { PerformanceMonitoring = performanceOptions });
    }

    public SemanticModelRepositoryOptions Build()
    {
        // Return immutable current state (no copying needed)
        return _current;
    }
}

/// <summary>Thread-safe immutable builder implementation for PerformanceMonitoringOptions.</summary>
public class PerformanceMonitoringOptionsBuilder : IPerformanceMonitoringOptionsBuilder
{
    private readonly PerformanceMonitoringOptions _current;

    // Private constructor - only used internally for immutable chaining
    private PerformanceMonitoringOptionsBuilder(PerformanceMonitoringOptions options)
    {
        _current = options;
    }

    // Public factory method
    public static IPerformanceMonitoringOptionsBuilder Create()
    {
        return new PerformanceMonitoringOptionsBuilder(new PerformanceMonitoringOptions());
    }

    public IPerformanceMonitoringOptionsBuilder EnableLocalMonitoring(bool enabled = true)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new PerformanceMonitoringOptionsBuilder(_current with { EnableLocalMonitoring = enabled });
    }

    public IPerformanceMonitoringOptionsBuilder WithMetricsRetention(TimeSpan retention)
    {
        // Create new instance instead of mutating current state (immutable pattern)
        return new PerformanceMonitoringOptionsBuilder(_current with { MetricsRetentionPeriod = retention });
    }

    public PerformanceMonitoringOptions Build()
    {
        // Return immutable current state (no copying needed)
        return _current;
    }
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

/// <summary>Performance monitor interface for repository operations. Implementation details are defined in the OpenTelemetry Application Monitoring Specification.</summary>
public interface IPerformanceMonitor : IDisposable
{
    IPerformanceTrackingContext StartOperation(string operationName, IDictionary<string, object>? metadata = null);
    Task<OperationStatistics?> GetOperationStatisticsAsync(string operationName);
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
    
    // BREAKING CHANGE: Make Find methods async to support lazy loading transparently
    Task<SemanticModelTable?> FindTableAsync(string schemaName, string tableName);
    Task<SemanticModelView?> FindViewAsync(string schemaName, string viewName);
    Task<SemanticModelStoredProcedure?> FindStoredProcedureAsync(string schemaName, string storedProcedureName);
    
    // Add async collection accessors for lazy loading scenarios
    Task<IEnumerable<SemanticModelTable>> GetTablesAsync();
    Task<IEnumerable<SemanticModelView>> GetViewsAsync();
    Task<IEnumerable<SemanticModelStoredProcedure>> GetStoredProceduresAsync();
    
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
- **AC-013**: Given SemanticModelRepositoryOptionsBuilder, When methods are chained, Then fluent interface maintains immutability until Build() is called
- **AC-014**: Given repository options, When LoadModelAsync is called with options object, Then all specified options are applied correctly
- **AC-015**: Given builder pattern usage, When Build() is called multiple times, Then each call returns a new immutable options instance
- **AC-016**: Given invalid option combinations, When Build() is called, Then appropriate validation exceptions are thrown
- **AC-017**: Given default builder usage, When no options are specified, Then safe defaults are applied (no lazy loading, no change tracking, no caching)
- **AC-022**: Given multiple threads using same builder instance, When concurrent method chaining occurs, Then no thread interference or configuration pollution occurs due to immutable builder pattern
- **AC-023**: Given builder instance stored in static field, When multiple threads access builder simultaneously, Then each thread gets independent configuration without cross-contamination
- **AC-018**: Given performance monitoring integration, When enabled, Then implementation follows the requirements and acceptance criteria defined in the [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md)
- **AC-019**: Given OpenTelemetry services are not configured, When repository operations are performed, Then full functionality is maintained with zero performance degradation and no errors or warnings related to telemetry
- **AC-020**: Given .NET Aspire environment variables are configured, When EnableAspireCompatibility is true, Then telemetry is automatically sent to the configured OTLP endpoint without additional configuration
- **AC-021**: Given OTEL_EXPORTER_OTLP_ENDPOINT environment variable is not set, When .NET Aspire compatibility is enabled, Then telemetry export is gracefully disabled and repository operations continue normally
- **AC-022**: Given lazy loading is enabled, When FindTableAsync is called, Then table is found using async collection loading without consumer knowledge of loading strategy
- **AC-023**: Given lazy loading is disabled, When FindTableAsync is called, Then table is found using synchronous collection with automatic fallback
- **AC-024**: Given existing synchronous Find method calls, When upgrading to new API, Then compilation errors guide migration to async methods with clear error messages
- **AC-025**: Given mixed lazy and eager loading scenarios, When async Find methods are called, Then correct loading strategy is automatically selected based on model configuration
- **AC-026**: Given semantic model storage format, When loading existing models, Then data format backward compatibility is maintained regardless of API changes

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
- Telemetry backend connectivity and failover testing
- Performance impact assessment for monitoring overhead
- Cloud telemetry integration testing with mock services

## 7. Rationale & Context

**Repository Pattern Selection**: Provides clean abstraction between domain logic and data access. Enables testability through mocking, flexibility for multiple persistence strategies, and maintainability through separation of concerns.

**Builder Pattern for Options Configuration**: Addresses the "Boolean Parameter Hell" problem by providing a fluent, self-documenting interface for configuring repository options. Improves code readability, extensibility, and maintainability while supporting complex configuration scenarios without breaking existing method signatures.

**Three-Strategy Persistence Design**:

- **Local Disk JSON**: Development scenarios, small deployments, version control integration, human-readable format
- **Azure Blob Storage JSON**: Cloud-native scenarios, scalable storage, cost-effective for large models, geo-replication support
- **Cosmos DB Documents**: Global distribution, low-latency access, automatic indexing, integrated with Azure ecosystem

**Hierarchical Structure with Index**: Separate entity files enable human readability, version control compatibility, lazy loading support, efficient partial updates, and fast model discovery. Index document provides fast model discovery and metadata access.

**JSON Serialization**: Selected for AI compatibility, human readability, language agnostic consumption, and extensive tooling ecosystem.

**Change Tracking**: Essential for performance optimization (selective persistence), conflict resolution, audit trails, and network optimization in distributed scenarios.

**Immutable Options Pattern**: Options objects are immutable after construction through the builder, preventing unintended modifications and ensuring thread safety. The builder pattern provides a clean separation between configuration construction and usage, following modern C# best practices and enabling safe concurrent access to options objects.

**Immutable Builder Pattern Design**: The specification implements the immutable builder pattern to address critical concurrency issues with traditional mutable builders. Key design decisions:

- **Thread Safety by Design**: Each builder method creates a new instance instead of mutating shared state, eliminating race conditions
- **Static Field Safety**: Multiple threads can safely share builder instances stored in static fields without interference
- **Record-Based Options**: Uses C# records with `init` properties for structural immutability and efficient copying with `with` expressions
- **Factory Methods**: Builder construction through static `Create()` methods instead of public constructors for controlled instantiation
- **Zero Shared Mutable State**: No internal fields are modified after builder creation, preventing configuration pollution between method chains
- **Memory Efficiency**: .NET 9 record copying is highly optimized, making the performance overhead of immutable instances negligible compared to the safety benefits

This approach prevents common concurrency bugs such as configuration pollution in multi-threaded scenarios, test interference, and unpredictable behavior when builder instances are shared between different parts of an application.

**Performance Monitoring Architecture**: Repository operations integrate with the monitoring framework defined in the [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md). The implementation ensures:

- **Zero External Dependencies**: When telemetry services are disabled or unavailable, the repository operates with full functionality and zero performance impact
- **Graceful Degradation**: Automatic detection and handling of missing telemetry infrastructure without throwing exceptions or logging errors
- **Multi-Backend Support**: Seamless integration with Azure Application Insights, .NET Aspire, Prometheus, Jaeger, and other OpenTelemetry-compatible backends when enabled and properly configured
- **.NET Aspire Compatibility**: Native support for .NET Aspire's OpenTelemetry configuration through standard environment variables (OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_SERVICE_NAME, etc.)
- **Flexible Configuration**: Runtime configuration of telemetry backends without requiring code changes or application restarts

## 8. Dependencies & External Integrations

### External Systems

- **EXT-001**: Source Database Systems - SQL Server, MySQL, PostgreSQL, or other relational databases that provide schema metadata through standard information schema views or system catalogs
- **EXT-002**: Authentication Providers - Azure Active Directory, Active Directory, or other identity providers for secure access to cloud resources

### Third-Party Services

- **SVC-001**: Azure Storage Account - Blob storage service with standard or premium performance tiers, supporting hierarchical namespace and access control for cloud persistence strategy
- **SVC-002**: Azure Cosmos DB - Multi-model database service with global distribution capabilities, supporting SQL API and hierarchical partition keys for document persistence strategy
- **SVC-003**: Azure Key Vault - Secret management service for secure storage and retrieval of connection strings, API keys, and other sensitive configuration data

### Infrastructure Dependencies

- **INF-001**: .NET 9 Runtime - Latest version of .NET runtime with C# 11+ language features, async/await patterns, and modern dependency injection capabilities
- **INF-002**: File System Access - Local disk storage with read/write permissions for development scenarios and local persistence strategy
- **INF-003**: Network Connectivity - Reliable internet connection for Azure service access, with appropriate firewall and proxy configurations
- **INF-004**: Azure Resource Group - Logical container for Azure resources with proper RBAC permissions and resource management policies

### Data Dependencies

- **DAT-001**: Database Schema Metadata - Access to information schema views, system catalogs, or equivalent metadata sources for schema extraction and semantic model generation
- **DAT-002**: Configuration Data - Application settings, connection strings, and environment-specific configuration accessible through .NET configuration providers
- **DAT-003**: Semantic Enhancement Data - Optional AI-generated descriptions, business rules, and metadata enrichments from generative AI services

### Technology Platform Dependencies

- **PLT-001**: JSON Serialization Library - System.Text.Json or compatible serialization framework supporting async operations and injection protection
- **PLT-002**: Azure SDK Libraries - Latest Azure client libraries for Blob Storage, Cosmos DB, Key Vault, and Identity services with DefaultAzureCredential support
- **PLT-003**: Dependency Injection Framework - Microsoft.Extensions.DependencyInjection or compatible DI container supporting service lifetime management and configuration options
- **PLT-004**: Logging Framework - Microsoft.Extensions.Logging or compatible structured logging framework for operational monitoring and diagnostics
- **PLT-005**: OpenTelemetry .NET SDK - Industry-standard observability framework for metrics, traces, and logs with vendor-neutral telemetry collection
- **PLT-006**: OpenTelemetry Exporters - Configurable exporters for Azure Application Insights, .NET Aspire, Prometheus, Jaeger, and other monitoring backends
- **PLT-007**: Performance Counters Library - System.Diagnostics.PerformanceCounter or equivalent for local system metrics collection
- **PLT-008**: .NET Aspire Integration - Optional integration with .NET Aspire's telemetry configuration through standard OpenTelemetry environment variables

### Compliance Dependencies

- **COM-001**: Data Privacy Regulations - GDPR, CCPA, or regional data protection requirements affecting semantic model storage and processing
- **COM-002**: Security Standards - Industry security frameworks (SOC 2, ISO 27001) governing cloud service usage and data handling practices
- **COM-003**: Organizational Policies - Corporate governance policies for cloud resource usage, data classification, and access control requirements

**Note**: This section focuses on architectural and business dependencies required for the semantic model repository pattern implementation. Specific package versions and implementation details are maintained separately in implementation documentation.

## 9. Examples & Edge Cases

### Basic Usage - Local Disk

```csharp
// BREAKING CHANGE: Find methods are now async for transparent lazy loading support
var provider = serviceProvider.GetRequiredService<ISemanticModelProvider>();
var repository = serviceProvider.GetRequiredService<ISemanticModelRepository>();
var model = await provider.ExtractSemanticModelAsync();
await repository.SaveModelAsync(model, new DirectoryInfo(@"C:\Models\Database"));

// Load with async Find methods (works with both lazy and eager loading)
var loadedModel = await repository.LoadModelAsync(
    new DirectoryInfo(@"C:\Models\Database"), 
    strategyName: "localdisk");

// BREAKING CHANGE: All Find operations are now async
var table = await loadedModel.FindTableAsync("dbo", "Customer");
var view = await loadedModel.FindViewAsync("dbo", "CustomerView");
var storedProc = await loadedModel.FindStoredProcedureAsync("dbo", "GetCustomer");
```

### Builder Pattern Usage (Recommended)

```csharp
// Using immutable builder pattern for thread-safe configurations
var provider = serviceProvider.GetRequiredService<ISemanticModelProvider>();
var repository = serviceProvider.GetRequiredService<ISemanticModelRepository>();

// Create and save semantic model
var model = await provider.ExtractSemanticModelAsync();
await repository.SaveModelAsync(model, new DirectoryInfo(@"C:\Models\Database"));

// Load with immutable builder pattern - basic configuration
var basicOptions = SemanticModelRepositoryOptionsBuilder.Create()
    .WithLazyLoading()
    .WithChangeTracking()
    .Build();
var loadedModel = await repository.LoadModelAsync(new DirectoryInfo(@"C:\Models\Database"), basicOptions);

// Load with immutable builder pattern - advanced configuration
var advancedOptions = SemanticModelRepositoryOptionsBuilder.Create()
    .WithLazyLoading(true)
    .WithChangeTracking(true)
    .WithCaching(true, TimeSpan.FromMinutes(30))
    .WithStrategyName("localdisk")
    .WithMaxConcurrentOperations(5)
    .Build();
var optimizedModel = await repository.LoadModelAsync(new DirectoryInfo(@"C:\Models\Database"), advancedOptions);

// Fluent interface for different scenarios - each chain is independent and thread-safe
var developmentOptions = SemanticModelRepositoryOptionsBuilder.Create()
    .WithLazyLoading()
    .WithPerformanceMonitoring(perf => perf
        .EnableLocalMonitoring()
        .WithMetricsRetention(TimeSpan.FromHours(8)))
    .Build();

var productionOptions = SemanticModelRepositoryOptionsBuilder.Create()
    .WithLazyLoading()
    .WithCaching(true, TimeSpan.FromMinutes(15))
    .WithPerformanceMonitoring(perf => perf
        .EnableLocalMonitoring()
        .WithMetricsRetention(TimeSpan.FromHours(24)))
    .Build();
```

### .NET Aspire Integration Examples

```csharp
// .NET Aspire automatic configuration - uses environment variables
var aspireOptions = optionsBuilder
    .WithLazyLoading()
    .WithPerformanceMonitoring(perf => perf
        .EnableLocalMonitoring()
        .EnableAspireCompatibility()
        .EnableOpenTelemetry())
    .Build();

// Explicit OTLP endpoint configuration for custom scenarios
var customOtlpOptions = optionsBuilder
    .WithPerformanceMonitoring(perf => perf
        .EnableLocalMonitoring()
        .EnableOpenTelemetry()
        .WithOtlpEndpoint("http://localhost:4318"))
    .Build();

// Repository operations remain unchanged regardless of telemetry configuration
var model = await repository.LoadModelAsync(modelPath, aspireOptions);
// Telemetry automatically flows to .NET Aspire dashboard when available
```

### Performance Monitoring Examples

```csharp
// Basic configuration - works without any telemetry infrastructure
var basicOptions = optionsBuilder
    .WithLazyLoading()
    .Build();

// .NET Aspire integration - automatically uses environment variables
var aspireOptions = optionsBuilder
    .WithLazyLoading()
    .Build();

// Repository operations work regardless of telemetry configuration
var monitor = serviceProvider.GetRequiredService<IPerformanceMonitor>();
using var context = monitor.StartOperation("LoadSemanticModel");
try
{
    var model = await repository.LoadModelAsync(modelPath, options);
    // Telemetry is sent if available, ignored if not configured
}
catch (Exception ex)
{
    // Errors are tracked locally regardless of cloud telemetry status
    throw;
}
```

**Key Benefits**:

- **Zero Configuration Required**: Repository works perfectly without any telemetry setup
- **No Performance Impact**: When telemetry is unavailable, there's no performance penalty
- **Automatic .NET Aspire Integration**: Respects standard OpenTelemetry environment variables
- **Graceful Degradation**: No exceptions or errors when telemetry services are unavailable

*Note*: For detailed performance monitoring implementation including OpenTelemetry integration, multi-backend support, and comprehensive observability configuration, see the [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md).

*Note*: For detailed performance monitoring implementation including OpenTelemetry integration, multi-backend support, and comprehensive observability configuration, see the [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md).

### Memory and Performance Optimization Examples

```csharp
var memoryOptimizedOptions = optionsBuilder
    .WithLazyLoading()
    .Build();

var performanceOptimizedOptions = optionsBuilder
    .WithChangeTracking()
    .Build();
```

### Azure Blob Storage Usage

```csharp
// Create and save to Azure Blob Storage using builder pattern
var blobStrategy = serviceProvider.GetRequiredService<IAzureBlobPersistenceStrategy>();
var repository = serviceProvider.GetRequiredService<ISemanticModelRepository>();
var optionsBuilder = serviceProvider.GetRequiredService<ISemanticModelRepositoryOptionsBuilder>();

blobStrategy.ConnectionString = "DefaultEndpointsProtocol=https;AccountName=...";
var model = await provider.ExtractSemanticModelAsync();
await blobStrategy.SaveModelAsync(model, "semantic-models", "adventureworks");

// Load from Azure Blob Storage with optimized settings
var azureOptions = optionsBuilder
    .WithLazyLoading()
    .WithCaching(true, TimeSpan.FromMinutes(45))
    .WithStrategyName("azureblob")
    .WithMaxConcurrentOperations(8)
    .Build();
var loadedModel = await repository.LoadModelAsync(new DirectoryInfo("adventureworks"), azureOptions);
```

### Cosmos DB Usage

```csharp
// Create and save to Cosmos DB using builder pattern
var cosmosDbStrategy = serviceProvider.GetRequiredService<ICosmosDbPersistenceStrategy>();
var repository = serviceProvider.GetRequiredService<ISemanticModelRepository>();
var optionsBuilder = serviceProvider.GetRequiredService<ISemanticModelRepositoryOptionsBuilder>();

cosmosDbStrategy.ConnectionString = "AccountEndpoint=https://...;AccountKey=...";
cosmosDbStrategy.PartitionKeyPath = "/partitionKey"; // Hierarchical partition key path
var model = await provider.ExtractSemanticModelAsync();
await cosmosDbStrategy.SaveModelAsync(model, "SemanticModels", "Models");

// Load from Cosmos DB with high-performance settings
var cosmosDbOptions = optionsBuilder
    .WithLazyLoading()
    .WithChangeTracking()
    .WithCaching(true, TimeSpan.FromHours(1))
    .WithStrategyName("CosmosDb")
    .WithMaxConcurrentOperations(12)
    .Build();
var loadedModel = await repository.LoadModelAsync(new DirectoryInfo("Models"), cosmosDbOptions);

// Query specific entity by partition key for optimal performance
// Partition key format: "{model-name}/{entity-type}/{entity-name}"
// Example: "adventureworks/table/Sales.Customer"
```

### Repository Pattern with Builder

```csharp
// Schema extraction with repository pattern and builder configuration
var schemaRepo = serviceProvider.GetRequiredService<ISchemaRepository>();
var repository = serviceProvider.GetRequiredService<ISemanticModelRepository>();
var optionsBuilder = serviceProvider.GetRequiredService<ISemanticModelRepositoryOptionsBuilder>();

var tables = await schemaRepo.GetTablesAsync("Sales");
var semanticTable = await schemaRepo.CreateSemanticModelTableAsync(tables.First().Value);

// Configure different loading strategies based on use case
var developmentOptions = optionsBuilder
    .WithLazyLoading()
    .WithChangeTracking()
    .WithStrategyName("localdisk")
    .Build();

var productionOptions = optionsBuilder
    .WithLazyLoading()
    .WithCaching(true, TimeSpan.FromMinutes(30))
    .WithStrategyName("azureblob")
    .WithMaxConcurrentOperations(10)
    .Build();

// Load model with appropriate configuration
var model = await repository.LoadModelAsync(modelPath, developmentOptions);
```

### Builder Pattern Validation

```csharp
// Builder with validation for option combinations
var optionsBuilder = serviceProvider.GetRequiredService<ISemanticModelRepositoryOptionsBuilder>();

try
{
    // This should work - valid combination
    var validOptions = optionsBuilder
        .WithLazyLoading()
        .WithChangeTracking()
        .WithCaching(true, TimeSpan.FromMinutes(30))
        .Build();
    
    // This should throw validation exception - invalid cache expiration
    var invalidOptions = optionsBuilder
        .WithCaching(true, TimeSpan.FromSeconds(-1))
        .Build();
}
catch (ArgumentException ex)
{
    // Handle validation errors
    _logger.LogError(ex, "Invalid repository options configuration");
}
```

### Builder Pattern Reuse (Thread-Safe)

```csharp
// Immutable builder instances are safe to use from static fields and across multiple threads
public static class RepositoryConfiguration
{
    // This is now SAFE for concurrent access due to immutable builder pattern
    private static readonly ISemanticModelRepositoryOptionsBuilder _baseBuilder 
        = SemanticModelRepositoryOptionsBuilder.Create();
    
    public static SemanticModelRepositoryOptions GetDevelopmentOptions()
    {
        return _baseBuilder  // Each call creates independent immutable chain
            .WithLazyLoading(true)
            .WithChangeTracking(true)
            .Build();
    }
    
    public static SemanticModelRepositoryOptions GetProductionOptions()
    {
        return _baseBuilder  // Completely independent of other calls
            .WithLazyLoading(false)
            .WithCaching(true)
            .Build();
    }
}

// Create different preset configurations - all thread-safe
var memoryOptimizedOptions = SemanticModelRepositoryOptionsBuilder.Create()
    .WithLazyLoading()
    .WithCaching(true, TimeSpan.FromHours(2))
    .Build();

var performanceOptimizedOptions = SemanticModelRepositoryOptionsBuilder.Create()
    .WithChangeTracking()
    .WithCaching()
    .WithMaxConcurrentOperations(15)
    .Build();

var fullFeaturedOptions = SemanticModelRepositoryOptionsBuilder.Create()
    .WithLazyLoading()
    .WithChangeTracking()
    .WithCaching(true, TimeSpan.FromMinutes(45))
    .WithMaxConcurrentOperations(8)
    .Build();

// Each Build() call creates a new immutable options instance
var model1 = await repository.LoadModelAsync(path1, memoryOptimizedOptions);
var model2 = await repository.LoadModelAsync(path2, performanceOptimizedOptions);
var model3 = await repository.LoadModelAsync(path3, fullFeaturedOptions);
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

## 10. Validation Criteria

**Functional**:

- Persist/retrieve semantic models without data loss across all three storage strategies
- Async operations complete within performance thresholds for Local Disk, Azure Blob Storage, and Cosmos DB
- Concurrent access without corruption across all persistence strategies
- Lazy loading reduces memory usage ≥70% for all storage types
- Change tracking identifies modifications with 100% accuracy
- Index document maintains referential integrity to entity documents/files
- Builder pattern provides fluent interface for options configuration
- Options objects are immutable after Build() method execution
- Multiple Build() calls from same builder instance produce independent options objects

**Performance**:

- Model extraction for 100 tables ≤30 seconds
- Entity loading ≤500 milliseconds (Local Disk), ≤2 seconds (Azure Blob), ≤1 second (Cosmos DB)
- Memory usage ≤2GB for 10,000 entities across all storage strategies
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
- Builder pattern registration in dependency injection container
- Options builder lifecycle management (singleton or scoped as appropriate)
- Graceful telemetry degradation without application impact
- .NET Aspire compatibility through standard OpenTelemetry environment variables
- Zero performance overhead when telemetry services are unavailable

## 11. Breaking Change Migration Guide

### API Changes Summary

This specification introduces breaking changes to improve lazy loading support and provide a cleaner, more consistent API.

**Before (v1.x):**

```csharp
var table = semanticModel.FindTable("dbo", "Customer");
var view = semanticModel.FindView("dbo", "CustomerView");
var storedProc = semanticModel.FindStoredProcedure("dbo", "GetCustomer");
```

**After (v2.0):**

```csharp
var table = await semanticModel.FindTableAsync("dbo", "Customer");
var view = await semanticModel.FindViewAsync("dbo", "CustomerView");
var storedProc = await semanticModel.FindStoredProcedureAsync("dbo", "GetCustomer");
```

### Consumer Updates Required

1. **Console Applications**: Update all Find method calls to async patterns
2. **Web APIs**: Ensure controller methods support async operations
3. **Service Classes**: Update service method signatures to async
4. **Unit Tests**: Convert test methods to async patterns with proper assertions

### Migration Benefits

- **Transparent Lazy Loading**: Find methods work seamlessly regardless of loading strategy
- **Performance**: No sync-over-async patterns - true async throughout
- **Consistency**: All I/O operations follow async patterns
- **Future-Proof**: Foundation for advanced caching and optimization features

### Data Format Compatibility

- **Storage Format**: Existing semantic model files remain fully compatible
- **Index Documents**: New format enhancements are backward compatible
- **Entity Files**: No changes to entity file structure or content

### Compatibility Strategy

- **Clean Break**: Remove synchronous Find methods entirely for clarity
- **Compilation Guidance**: Clear compiler errors guide migration process
- **Documentation**: Comprehensive migration examples for all scenarios
- **Testing Support**: Migration validation through existing test suites

## 12. Related Specifications / Further Reading

- [Azure Application Insights OpenTelemetry Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md)
- [Infrastructure Deployment Bicep AVM Specification](./infrastructure-deployment-bicep-avm.md)
- [Microsoft .NET Application Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Repository Pattern Documentation](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Entity Framework Core Change Tracking](https://docs.microsoft.com/en-us/ef/core/change-tracking/)
- [System.Text.Json Serialization Guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
