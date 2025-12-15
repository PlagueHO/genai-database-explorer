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

**Assumptions**: .NET 10, dependency injection, async patterns, JSON serialization.

## 2. Definitions

- **Repository Pattern**: Encapsulates data access logic with uniform interface
- **Semantic Model**: AI-consumable database schema representation with metadata and relationships
- **Schema Repository**: Extracts/transforms raw database schema information
- **Lazy Loading**: Defers data loading until needed, reduces memory usage
- **Dirty Tracking**: Monitors object changes for selective persistence
- **Unit of Work**: Manages related operations as single transaction

## 3. Requirements, Constraints & Guidelines

### Core Requirements

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

### Security Requirements

- **SEC-001**: Path validation prevents directory traversal
- **SEC-002**: Entity name sanitization for file paths
- **SEC-003**: Authentication for persistence operations (Azure: Entra ID with `DefaultAzureCredential`; Shared Key access disabled)
- **SEC-004**: Secure handling of connection strings (Key Vault optional; fall back to Entra ID when not provided)
- **SEC-005**: JSON serialization injection protection

**Azure Blob Storage Implementation Notes:**

- Uses Entra ID-first via `DefaultAzureCredential` to create `BlobServiceClient`
- Storage account policy requires `allowSharedKeyAccess=false`; Shared Key auth is not used
- Optional: a secure connection string can be retrieved from Azure Key Vault at runtime to refresh clients; if unavailable, the strategy continues with Entra ID
- Client creation hooks for testability: `CreateBlobClientOptions()`, `CreateDefaultCredential()`, `CreateBlobServiceClient()`, `CreateBlobContainerClient()`, `GetBlobClient()`
- Download wrapper: `DownloadContentAsync(BlobClient, CancellationToken)` enables deterministic unit testing
- Existence checks: `ExistsAsync(DirectoryInfo)` validate the model name using `EntityNameSanitizer`

### Performance Requirements

- **PER-001**: Concurrent operations without corruption
- **PER-002**: Entity loading ≤5s for 1000 entities
- **PER-003**: Efficient caching mechanisms
- **PER-004**: Memory optimization via lazy loading
- **PER-005**: Performance monitoring that extends the core requirements defined in the [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md) with repository-specific metrics and operations, ensuring zero performance degradation when telemetry services are unavailable

### Constraints

- **CON-001**: .NET 10 compatibility
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
/// <summary>Repository for semantic model persistence with flexible loading options.</summary>
public interface ISemanticModelRepository
{
    Task SaveModelAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null);
    Task SaveChangesAsync(SemanticModel model, DirectoryInfo modelPath, string? strategyName = null);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, string? strategyName = null);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath, SemanticModelRepositoryOptions options);
}

/// <summary>Persistence strategy interface for different storage providers.</summary>
public interface ISemanticModelPersistenceStrategy
{
    Task SaveModelAsync(SemanticModel semanticModel, DirectoryInfo modelPath);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath);
    Task<bool> ExistsAsync(DirectoryInfo modelPath);
    Task<IEnumerable<string>> ListModelsAsync(DirectoryInfo rootPath);
    Task DeleteModelAsync(DirectoryInfo modelPath);
}

/// <summary>Azure Blob Storage persistence strategy.</summary>
public interface IAzureBlobPersistenceStrategy : ISemanticModelPersistenceStrategy, IDisposable
{
}

/// <summary>Cosmos DB persistence strategy.</summary>
public interface ICosmosDbPersistenceStrategy : ISemanticModelPersistenceStrategy, IDisposable
{
}
```

### Configuration Options

```csharp
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
```

### Builder Interfaces

```csharp
/// <summary>Builder for creating SemanticModelRepositoryOptions with fluent interface.</summary>
public interface ISemanticModelRepositoryOptionsBuilder
{
    ISemanticModelRepositoryOptionsBuilder WithLazyLoading(bool enabled = true);
    ISemanticModelRepositoryOptionsBuilder WithChangeTracking(bool enabled = true);
    ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled = true);
    ISemanticModelRepositoryOptionsBuilder WithCaching(bool enabled, TimeSpan expiration);
    ISemanticModelRepositoryOptionsBuilder WithStrategyName(string strategyName);
    ISemanticModelRepositoryOptionsBuilder WithMaxConcurrentOperations(int maxOperations);
    ISemanticModelRepositoryOptionsBuilder WithPerformanceMonitoring(Func<IPerformanceMonitoringOptionsBuilder, IPerformanceMonitoringOptionsBuilder> configure);
    SemanticModelRepositoryOptions Build();
}

/// <summary>Builder for performance monitoring configuration.</summary>
public interface IPerformanceMonitoringOptionsBuilder
{
    IPerformanceMonitoringOptionsBuilder EnableLocalMonitoring(bool enabled = true);
    IPerformanceMonitoringOptionsBuilder WithMetricsRetention(TimeSpan retention);
    PerformanceMonitoringOptions Build();
}

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

#### Semantic Entity Schema

**All persistence strategies use a unified semantic entity schema** for individual entity documents (tables, views, stored procedures). This schema includes versioning support for future evolution.

**Schema Version 1 (Current):**

```json
{
  "version": 1,
  "data": {
    // The actual entity data (table, view, or stored procedure definition)
    "Schema": "SalesLT",
    "Name": "Address",
    "Description": "Customer address information",
    "SemanticDescription": "AI-generated semantic description...",
    "Columns": [...],
    "Details": "Additional details...",
    "AdditionalInformation": "..."
  },
  "embedding": {
    // Optional embedding information - present only when vectors are generated
    "vector": [0.1234, 0.5678, ...],  // Float array of embedding values
    "metadata": {
      "modelId": "Embeddings",
      "dimensions": 1536,
      "contentHash": "abc123...",
      "generatedAt": "2025-09-07T10:30:00Z",
      "serviceId": "Embeddings",
      "version": "1"
    }
  }
}
```

**Legacy Schema Support (Backward Compatibility):**

The system supports two legacy formats for backward compatibility:

1. **Legacy Direct Format** (no envelope): Entity data stored directly as JSON without version or embedding wrapper
2. **Legacy Envelope Format** (without version): `{ "data": {...}, "embedding": {...} }` without version field

**Schema Evolution Rules:**

- New schema versions will increment the `version` field
- All loaders must support reading previous versions
- All savers use the current version schema
- Version field is mandatory for new schemas (version 1+)

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
- **AC-018**: Given performance monitoring integration, When enabled, Then implementation follows the requirements and acceptance criteria defined in the [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md)
- **AC-019**: Given OpenTelemetry services are not configured, When repository operations are performed, Then full functionality is maintained with zero performance degradation and no errors or warnings related to telemetry
- **AC-020**: Given .NET Aspire environment variables are configured, When EnableAspireCompatibility is true, Then telemetry is automatically sent to the configured OTLP endpoint without additional configuration
- **AC-021**: Given any semantic model entity, When SaveModelAsync is called, Then entity is persisted using the current versioned schema format (version 1) regardless of whether embeddings are present
- **AC-022**: Given legacy format entities (direct or envelope without version), When LoadModelAsync is called, Then entities are successfully loaded with backward compatibility

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

**Immutable Options Pattern**: Options objects are immutable after construction through the builder, preventing unintended modifications and ensuring thread safety. The builder pattern provides a clean separation between configuration construction and usage, following modern C# best practices and enabling safe concurrent access to options objects.

**Performance Monitoring Architecture**: Repository operations integrate with the monitoring framework defined in the [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md). The implementation ensures zero external dependencies and graceful degradation when telemetry services are disabled.

## 8. Dependencies & External Integrations

### Technology Platform Dependencies

- **PLT-001**: JSON Serialization Library - System.Text.Json or compatible serialization framework supporting async operations and injection protection
- **PLT-002**: Azure SDK Libraries - Latest Azure client libraries for Blob Storage, Cosmos DB, Key Vault, and Identity services with DefaultAzureCredential support
- **PLT-003**: Dependency Injection Framework - Microsoft.Extensions.DependencyInjection or compatible DI container supporting service lifetime management and configuration options
- **PLT-004**: Logging Framework - Microsoft.Extensions.Logging or compatible structured logging framework for operational monitoring and diagnostics
- **PLT-005**: OpenTelemetry .NET SDK - Industry-standard observability framework for metrics, traces, and logs with vendor-neutral telemetry collection

### External Systems

- **EXT-001**: Source Database Systems - SQL Server, MySQL, PostgreSQL, or other relational databases that provide schema metadata
- **EXT-002**: Authentication Providers - Azure Active Directory, Active Directory, or other identity providers for secure access to cloud resources

### Third-Party Services

- **SVC-001**: Azure Storage Account - Blob storage service with standard or premium performance tiers, supporting hierarchical namespace and access control
- **SVC-002**: Azure Cosmos DB - Multi-model database service with global distribution capabilities, supporting SQL API and hierarchical partition keys
- **SVC-003**: Azure Key Vault - Secret management service for secure storage and retrieval of connection strings and sensitive configuration data

## 9. Examples & Usage Patterns

### Basic Repository Usage

```csharp
// Load with default options
var repository = serviceProvider.GetRequiredService<ISemanticModelRepository>();
var model = await repository.LoadModelAsync(new DirectoryInfo(@"C:\Models\Database"));

// Save model
await repository.SaveModelAsync(model, new DirectoryInfo(@"C:\Models\Database"));

// Save only changes (requires change tracking)
await repository.SaveChangesAsync(model, new DirectoryInfo(@"C:\Models\Database"));
```

### Builder Pattern Usage

```csharp
// Basic configuration with builder pattern
var options = SemanticModelRepositoryOptionsBuilder.Create()
    .WithLazyLoading()
    .WithChangeTracking()
    .Build();

var model = await repository.LoadModelAsync(new DirectoryInfo(@"C:\Models\Database"), options);

// Advanced configuration
var productionOptions = SemanticModelRepositoryOptionsBuilder.Create()
    .WithLazyLoading()
    .WithCaching(true, TimeSpan.FromMinutes(15))
    .WithMaxConcurrentOperations(20)
    .WithPerformanceMonitoring(perf => perf
        .EnableLocalMonitoring()
        .WithMetricsRetention(TimeSpan.FromHours(24)))
    .Build();

var optimizedModel = await repository.LoadModelAsync(modelPath, productionOptions);
```

### Strategy-Specific Usage

```csharp
// Local Disk strategy
await repository.SaveModelAsync(model, modelPath, "LocalDisk");

// Azure Blob strategy
await repository.SaveModelAsync(model, modelPath, "AzureBlob");

// Cosmos DB strategy
await repository.SaveModelAsync(model, modelPath, "CosmosDb");
```

## 10. Validation Criteria

**Functional**:

- Persist/retrieve semantic models without data loss across all three storage strategies
- Async operations complete within performance thresholds for Local Disk, Azure Blob Storage, and Cosmos DB
- Concurrent access without corruption across all persistence strategies
- Lazy loading reduces memory usage ≥70% for all storage types
- Change tracking identifies modifications with 100% accuracy
- Builder pattern provides fluent interface for options configuration
- Options objects are immutable after Build() method execution

**Performance**:

- Model extraction for 100 tables ≤30 seconds
- Entity loading ≤500 milliseconds (Local Disk), ≤2 seconds (Azure Blob), ≤1 second (Cosmos DB)
- Memory usage ≤2GB for 10,000 entities across all storage strategies
- Cosmos DB queries utilize hierarchical partition key for optimal performance
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
- Graceful telemetry degradation without application impact
- .NET Aspire compatibility through standard OpenTelemetry environment variables
- Zero performance overhead when telemetry services are unavailable

## 11. Related Specifications / Further Reading

- [OpenTelemetry Application Monitoring Specification](./spec-monitoring-azure-application-insights-opentelemetry.md)
- [Infrastructure Deployment Bicep AVM Specification](./spec-infrastructure-deployment-bicep-avm.md)
- [Microsoft .NET Application Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Repository Pattern Documentation](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Entity Framework Core Change Tracking](https://docs.microsoft.com/en-us/ef/core/change-tracking/)
- [System.Text.Json Serialization Guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
