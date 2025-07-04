---
title: Semantic Model Repository - Technical Documentation
component_path: src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository
version: 1.0
date_created: 2025-07-05
last_updated: 2025-07-05
owner: GenAI Database Explorer Team
tags: [component, repository, persistence, architecture, semantic-model, strategy-pattern, caching, async]
---

## Semantic Model Repository Documentation

The Semantic Model Repository component provides a unified abstraction layer for persisting and retrieving semantic models across different storage backends. It implements the Repository Pattern with Strategy Pattern for multiple persistence backends, supporting advanced features like lazy loading, change tracking, caching, and concurrent operation protection.

## 1. Component Overview

### Purpose/Responsibility

- **OVR-001**: **Primary Responsibility** - Provide unified persistence operations for semantic models across multiple storage backends (Local Disk, Azure Blob Storage, Azure Cosmos DB)
- **OVR-002**: **Scope** - Includes CRUD operations, caching, lazy loading, change tracking, concurrent operation protection, and security validation. Excludes business logic for semantic model creation/transformation
- **OVR-003**: **System Context** - Acts as the persistence layer between the semantic model domain objects and various storage systems, integrating with the broader GenAI Database Explorer ecosystem

### Key Capabilities

- Unified repository interface abstracting storage implementation details
- Multiple persistence strategies (Local Disk JSON, Azure Blob Storage, Azure Cosmos DB)
- Optional caching layer for performance optimization
- Lazy loading support for memory efficiency
- Change tracking for selective persistence
- Thread-safe concurrent operations with semaphore-based locking
- Comprehensive security validation and input sanitization
- Asynchronous operations throughout for I/O performance

## 2. Architecture Section

### Design Patterns

- **ARC-001**: **Repository Pattern** - Encapsulates data access logic and provides a uniform interface for semantic model persistence
- **ARC-002**: **Strategy Pattern** - Enables runtime selection of persistence strategies (Local Disk, Azure Blob, Cosmos DB)
- **ARC-003**: **Factory Pattern** - `PersistenceStrategyFactory` manages strategy instantiation and selection
- **ARC-004**: **Dependency Injection Pattern** - All dependencies injected via constructor for testability and flexibility
- **ARC-005**: **Disposable Pattern** - Proper resource cleanup for semaphores and strategy-specific resources

### Dependencies

- **Internal Dependencies**:
  - `GenAIDBExplorer.Core.Models.SemanticModel` - Core semantic model domain objects
  - `GenAIDBExplorer.Core.Security` - Security validation utilities (PathValidator, EntityNameSanitizer)
  - `GenAIDBExplorer.Core.Models.SemanticModel.ChangeTracking` - Change tracking infrastructure
- **External Dependencies**:
  - `Microsoft.Extensions.Logging` - Structured logging framework
  - `Microsoft.Extensions.Caching.Memory` - In-memory caching implementation
  - `Azure.Storage.Blobs` - Azure Blob Storage client SDK
  - `Microsoft.Azure.Cosmos` - Azure Cosmos DB client SDK
  - `Azure.Identity` - Azure authentication and credential management
  - `System.Text.Json` - JSON serialization for local disk persistence

### Component Interactions

The repository acts as a facade coordinating between multiple subsystems: strategy factory for backend selection, cache for performance, security validators for input validation, and logging for observability.

### Component Structure and Dependencies Diagram

```mermaid
graph TB
    subgraph "Client Layer"
        Client[Client Applications]
        Console[Console Application]
        API[API Services]
    end
    
    subgraph "Repository Layer"
        IRepo[ISemanticModelRepository]
        Repo[SemanticModelRepository]
        
        IRepo --> Repo
    end
    
    subgraph "Strategy Layer"
        IFactory[IPersistenceStrategyFactory]
        Factory[PersistenceStrategyFactory]
        
        IStrategy[ISemanticModelPersistenceStrategy]
        ILocal[ILocalDiskPersistenceStrategy]
        IAzure[IAzureBlobPersistenceStrategy]
        ICosmos[ICosmosPersistenceStrategy]
        
        IFactory --> Factory
        IStrategy --> ILocal
        IStrategy --> IAzure
        IStrategy --> ICosmos
        
        Local[LocalDiskPersistenceStrategy]
        Azure[AzureBlobPersistenceStrategy]
        Cosmos[CosmosPersistenceStrategy]
        
        ILocal --> Local
        IAzure --> Azure
        ICosmos --> Cosmos
    end
    
    subgraph "Caching Layer"
        ICache[ISemanticModelCache]
        Cache[MemorySemanticModelCache]
        CacheOpts[CacheOptions]
        
        ICache --> Cache
        Cache --> CacheOpts
    end
    
    subgraph "Configuration"
        AzureConfig[AzureBlobStorageConfiguration]
        CosmosConfig[CosmosDbConfiguration]
    end
    
    subgraph "Security"
        PathVal[PathValidator]
        EntitySan[EntityNameSanitizer]
    end
    
    subgraph "Storage Backends"
        LocalFS[Local File System]
        AzureBlob[Azure Blob Storage]
        CosmosDB[Azure Cosmos DB]
    end
    
    subgraph "Domain Models"
        SemanticModel[SemanticModel]
        ChangeTracker[ChangeTracker]
        LazyProxy[LazyLoadingProxy]
    end
    
    %% Client connections
    Client --> IRepo
    Console --> IRepo
    API --> IRepo
    
    %% Repository connections
    Repo --> IFactory
    Repo --> ICache
    Repo --> PathVal
    Repo --> EntitySan
    
    %% Factory connections
    Factory --> ILocal
    Factory --> IAzure
    Factory --> ICosmos
    
    %% Strategy connections
    Azure --> AzureConfig
    Cosmos --> CosmosConfig
    
    %% Storage connections
    Local --> LocalFS
    Azure --> AzureBlob
    Cosmos --> CosmosDB
    
    %% Domain connections
    Repo --> SemanticModel
    SemanticModel --> ChangeTracker
    SemanticModel --> LazyProxy
    
    %% Styling
    classDef interface fill:#e1f5fe,stroke:#01579b,stroke-width:2px
    classDef implementation fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef config fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef storage fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    classDef domain fill:#fce4ec,stroke:#880e4f,stroke-width:2px
    classDef security fill:#fff8e1,stroke:#f57f17,stroke-width:2px
    
    class IRepo,IFactory,IStrategy,ILocal,IAzure,ICosmos,ICache interface
    class Repo,Factory,Local,Azure,Cosmos,Cache implementation
    class AzureConfig,CosmosConfig,CacheOpts config
    class LocalFS,AzureBlob,CosmosDB storage
    class SemanticModel,ChangeTracker,LazyProxy domain
    class PathVal,EntitySan security
```

## 3. Interface Documentation

### ISemanticModelRepository

**Purpose**: Main repository interface providing unified access to semantic model persistence operations across different storage backends.

**Key Features**:

- Asynchronous CRUD operations with configurable strategies
- Support for lazy loading, change tracking, and caching options
- Concurrent operation safety with built-in locking mechanisms

| Method | Purpose | Parameters | Return Type | Usage Notes |
|--------|---------|------------|-------------|-------------|
| `SaveModelAsync` | Saves complete semantic model | `model`, `modelPath`, `strategyName?` | `Task` | Full model persistence |
| `SaveChangesAsync` | Saves only changed entities | `model`, `modelPath`, `strategyName?` | `Task` | Optimized for change tracking |
| `LoadModelAsync` | Loads semantic model (basic) | `modelPath`, `strategyName?` | `Task<SemanticModel>` | Basic loading without features |
| `LoadModelAsync` | Loads with lazy loading | `modelPath`, `enableLazyLoading`, `strategyName?` | `Task<SemanticModel>` | Memory-optimized loading |
| `LoadModelAsync` | Loads with lazy loading + change tracking | `modelPath`, `enableLazyLoading`, `enableChangeTracking`, `strategyName?` | `Task<SemanticModel>` | Feature combination |
| `LoadModelAsync` | Loads with all features | `modelPath`, `enableLazyLoading`, `enableChangeTracking`, `enableCaching`, `strategyName?` | `Task<SemanticModel>` | Full feature set |

### ISemanticModelPersistenceStrategy

**Purpose**: Base interface defining standard persistence operations that all storage backend strategies must implement.

| Method | Purpose | Parameters | Return Type | Usage Notes |
|--------|---------|------------|-------------|-------------|
| `SaveModelAsync` | Saves model to storage | `semanticModel`, `modelPath` | `Task` | Strategy-specific implementation |
| `LoadModelAsync` | Loads model from storage | `modelPath` | `Task<SemanticModel>` | Strategy-specific loading |
| `ExistsAsync` | Checks model existence | `modelPath` | `Task<bool>` | Non-destructive existence check |
| `ListModelsAsync` | Lists available models | `rootPath` | `Task<IEnumerable<string>>` | Discovery operation |
| `DeleteModelAsync` | Removes model | `modelPath` | `Task` | Destructive operation |

### ISemanticModelCache

**Purpose**: Caching interface for performance optimization of frequently accessed semantic models.

| Method | Purpose | Parameters | Return Type | Usage Notes |
|--------|---------|------------|-------------|-------------|
| `GetAsync` | Retrieves cached model | `cacheKey` | `Task<SemanticModel?>` | Returns null if not found |
| `SetAsync` | Caches model | `cacheKey`, `model`, `expiration?` | `Task` | Optional expiration time |
| `RemoveAsync` | Removes cached item | `cacheKey` | `Task<bool>` | Returns success status |
| `ClearAsync` | Clears entire cache | none | `Task` | Administrative operation |
| `GetStatisticsAsync` | Returns cache metrics | none | `Task<CacheStatistics>` | Performance monitoring |
| `ExistsAsync` | Checks cache key | `cacheKey` | `Task<bool>` | Non-destructive check |

## 4. Implementation Details

### SemanticModelRepository (Main Implementation)

**Responsibilities**:

- **IMP-001**: Orchestrates persistence operations across different strategies
- **IMP-002**: Provides concurrent operation protection using semaphores
- **IMP-003**: Integrates caching, security validation, and logging concerns
- **IMP-004**: Manages strategy selection through factory pattern

**Key Components**:

- `IPersistenceStrategyFactory` - Strategy selection and instantiation
- `ISemanticModelCache` - Optional caching layer for performance
- `ConcurrentDictionary<string, SemaphoreSlim>` - Path-specific concurrency control
- `SemaphoreSlim` - Global concurrency limiting

### Configuration Requirements

**Local Disk Strategy**:

- No specific configuration required (uses default local file system)
- Automatic directory creation and path validation
- JSON serialization with UTF-8 encoding

**Azure Blob Storage Strategy**:

```json
{
  "SemanticModelRepository": {
    "AzureBlobStorage": {
      "AccountEndpoint": "https://mystorageaccount.blob.core.windows.net",
      "ContainerName": "semantic-models",
      "BlobPrefix": "models/",
      "OperationTimeoutSeconds": 300,
      "MaxConcurrentOperations": 4
    }
  }
}
```

**Azure Cosmos DB Strategy**:

```json
{
  "SemanticModelRepository": {
    "CosmosDb": {
      "AccountEndpoint": "https://mycosmosaccount.documents.azure.com:443/",
      "DatabaseName": "SemanticModels",
      "ModelsContainerName": "Models",
      "EntitiesContainerName": "ModelEntities",
      "DatabaseThroughput": 400,
      "ConsistencyLevel": "Session"
    }
  }
}
```

### Key Algorithms

**Concurrent Operation Protection**:

1. Generate sanitized path key using `PathValidator`
2. Acquire path-specific semaphore from `ConcurrentDictionary`
3. Acquire global semaphore to limit total concurrent operations
4. Execute operation with timeout and error handling
5. Release semaphores in reverse order

**Cache Key Generation**:

- Uses SHA256 hash of normalized model path for consistent cache keys
- Includes strategy name in cache key to prevent cross-strategy conflicts

**Performance Characteristics**:

- **Memory**: Lazy loading can reduce memory usage by 70%+ for large models
- **I/O**: Asynchronous operations prevent thread blocking
- **Concurrency**: Semaphore-based limiting prevents resource exhaustion
- **Caching**: Can improve frequently accessed model load times by 80%+

## 5. Usage Examples

### Basic Usage

```csharp
// Dependency injection setup
services.AddScoped<ISemanticModelRepository, SemanticModelRepository>();
services.AddScoped<IPersistenceStrategyFactory, PersistenceStrategyFactory>();

// Basic repository operations
var repository = serviceProvider.GetRequiredService<ISemanticModelRepository>();

// Save a model (uses default strategy)
await repository.SaveModelAsync(model, new DirectoryInfo("/path/to/model"));

// Load a model
var loadedModel = await repository.LoadModelAsync(new DirectoryInfo("/path/to/model"));
```

### Advanced Usage with Features

```csharp
// Load with all advanced features enabled
var model = await repository.LoadModelAsync(
    new DirectoryInfo("/path/to/model"),
    enableLazyLoading: true,        // Memory optimization
    enableChangeTracking: true,     // Selective persistence
    enableCaching: true,            // Performance optimization
    strategyName: "azureblob"       // Specific storage backend
);

// Make changes to the model
model.AddTable(newTable);
model.UpdateView(existingView);

// Save only the changes (if change tracking is enabled)
await repository.SaveChangesAsync(model, new DirectoryInfo("/path/to/model"));
```

### Strategy-Specific Usage

```csharp
// Use specific persistence strategies
await repository.SaveModelAsync(model, modelPath, strategyName: "localdisk");
await repository.SaveModelAsync(model, modelPath, strategyName: "azureblob");
await repository.SaveModelAsync(model, modelPath, strategyName: "cosmos");

// List available models with specific strategy
var strategy = strategyFactory.GetStrategy("azureblob");
var availableModels = await strategy.ListModelsAsync(rootPath);

// Check if model exists before loading
if (await strategy.ExistsAsync(modelPath))
{
    var model = await repository.LoadModelAsync(modelPath, strategyName: "azureblob");
}
```

**Best Practices**:

- **USE-001**: Always use dependency injection for repository and factory instances
- **USE-002**: Enable caching for frequently accessed models in production
- **USE-003**: Use `SaveChangesAsync()` with change tracking for large models to improve performance

## 6. Quality Attributes

### Security (QUA-001)

**Input Validation**:

- Path validation prevents directory traversal attacks using `PathValidator`
- Entity name sanitization prevents file system injection via `EntityNameSanitizer`
- All public methods validate input parameters for null/empty values

**Authentication & Authorization**:

- Azure strategies use `DefaultAzureCredential` supporting managed identities
- Customer-managed encryption keys supported for Azure Blob Storage
- Secure credential handling without exposing connection strings in logs

**Data Protection**:

- Atomic file operations prevent data corruption during concurrent access
- Temporary file strategy ensures clean rollback on operation failures

### Performance (QUA-002)

**Scalability**:

- Concurrent operation limiting prevents resource exhaustion
- Lazy loading reduces memory footprint by 70%+ for large models
- Caching layer can improve load times by 80%+ for frequently accessed models

**Resource Usage**:

- Configurable semaphore limits for concurrent operations (default: 10)
- Memory cache with size limits and automatic expiration
- Asynchronous operations throughout prevent thread blocking

### Reliability (QUA-003)

**Error Handling**:

- Comprehensive exception handling with structured logging context
- Retry policies for transient cloud service failures
- Graceful degradation when optional features (caching) fail

**Fault Tolerance**:

- Path-specific locking prevents concurrent operation conflicts
- Atomic operations ensure data consistency during failures
- Resource cleanup via `IDisposable` pattern

### Maintainability (QUA-004)

**Code Standards**:

- Follows SOLID principles with clear separation of concerns
- Comprehensive XML documentation on all public APIs
- Consistent async/await patterns throughout

**Testing**:

- Unit tests with AAA pattern using MSTest, FluentAssertions, and Moq
- Mock-based testing for all persistence strategies
- Comprehensive test coverage including concurrent operations and security scenarios

### Extensibility (QUA-005)

**Extension Points**:

- New persistence strategies can be added by implementing `ISemanticModelPersistenceStrategy`
- Custom caching implementations via `ISemanticModelCache` interface
- Strategy factory supports runtime registration of new strategies
- Configuration-driven strategy selection

## 7. Reference Information

### Dependencies (REF-001)

**Core Dependencies**:

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.6" />
<PackageReference Include="System.Text.Json" Version="9.0.0" />
```

**Cloud Dependencies**:

```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.23.1" />
<PackageReference Include="Microsoft.Azure.Cosmos" Version="3.47.0" />
<PackageReference Include="Azure.Identity" Version="1.13.2" />
```

### Configuration Options (REF-002)

**Repository Configuration**:

```json
{
  "PersistenceStrategy": "LocalDisk",  // Default strategy
  "SemanticModelCache": {
    "Enabled": true,
    "MaxCacheSize": 100,
    "DefaultExpiration": "00:30:00",
    "MemoryLimitMB": 512
  }
}
```

### Testing Guidelines (REF-003)

**Unit Test Setup**:

```csharp
// Mock setup for repository testing
var mockFactory = new Mock<IPersistenceStrategyFactory>();
var mockStrategy = new Mock<ISemanticModelPersistenceStrategy>();
var mockCache = new Mock<ISemanticModelCache>();
var mockLogger = new Mock<ILogger<SemanticModelRepository>>();

mockFactory.Setup(f => f.GetStrategy(It.IsAny<string>())).Returns(mockStrategy.Object);

var repository = new SemanticModelRepository(
    mockFactory.Object, 
    mockLogger.Object, 
    cache: mockCache.Object
);
```

### Troubleshooting (REF-004)

**Common Issues**:

| Error | Cause | Solution |
|-------|-------|----------|
| `ArgumentException: Persistence strategy 'X' is not registered` | Strategy not registered in DI | Verify strategy registration in `HostBuilderExtensions` |
| `UnauthorizedAccessException` | Insufficient file system permissions | Check directory permissions for local disk strategy |
| `Azure.RequestFailedException` | Azure authentication failure | Verify Azure credentials and service permissions |
| `ObjectDisposedException` | Repository used after disposal | Ensure repository lifetime matches usage scope |

### Related Documentation (REF-005)

- [Implementation Plan](../../plan/plan-data-semantic-model-repository-updates.md) - Detailed implementation roadmap
- [Repository Pattern Specification](../../spec/spec-data-semantic-model-repository.md) - Architecture specification
- [Security Guidelines](../security/README.md) - Security best practices
- [Performance Optimization Guide](../performance/README.md) - Performance tuning recommendations

### Change History (REF-006)

**Version 1.0 (2025-07-05)**:

- Initial implementation with local disk, Azure Blob, and Cosmos DB strategies
- Lazy loading, change tracking, and caching features
- Concurrent operation protection and security hardening
- Comprehensive test coverage and documentation

**Migration Notes**:

- Backward compatible with existing `SemanticModel.SaveModelAsync()` and `LoadModelAsync()` methods
- New features are opt-in and don't affect existing functionality
- Configuration-driven strategy selection allows gradual migration to cloud storage
