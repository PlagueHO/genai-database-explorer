---
goal: Data Semantic Model Repository Updates - Implement Missing Persistence Strategy Pattern and Advanced Features
version: 1.0
date_created: 2025-06-28
last_updated: 2025-06-28
owner: GenAI Database Explorer Team
tags: [data, repository, persistence, semantic-model, feature, architecture]
---

# Introduction

This plan implements the missing requirements from the Data Semantic Model Repository Pattern Specification. The current implementation only supports basic local disk persistence through direct `SemanticModel.SaveModelAsync()` and `LoadModelAsync()` methods. This plan adds the repository pattern abstraction with three persistence strategies (Local Disk JSON, Azure Blob Storage, Cosmos DB), lazy loading, dirty tracking, security enhancements, and performance optimizations.

## 1. Requirements & Constraints

### Core Requirements

- **REQ-001**: Repository pattern abstraction for semantic model persistence
- **REQ-002**: Async operations for all I/O activities (already implemented)
- **REQ-003**: Support three specific persistence strategies: Local Disk JSON files, Azure Storage Blob Storage JSON files, and Cosmos DB Documents
- **REQ-004**: Hierarchical structure with separate entity files (partially implemented)
- **REQ-005**: CRUD operations for semantic models
- **REQ-006**: Dependency injection integration
- **REQ-007**: Error handling and logging (partially implemented)
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
- **CON-004**: Backward compatibility with existing local disk format - **CRITICAL**: All existing APIs must continue to function
- **CON-005**: Entity names ≤128 characters
- **CON-006**: No breaking changes to public APIs during implementation phases

### Guidelines

- **GUD-001**: Modern C# features (primary constructors, nullable types)
- **GUD-002**: SOLID principles
- **GUD-003**: Structured logging
- **GUD-004**: Consistent async/await patterns
- **GUD-005**: Repository pattern separation of concerns

### Patterns

- **PAT-001**: Strategy pattern for persistence implementations
- **PAT-002**: Lazy loading pattern for entity access
- **PAT-003**: Unit of Work pattern for change tracking
- **PAT-004**: Factory pattern for persistence strategy selection
- **PAT-005**: Adapter pattern to wrap existing functionality without breaking it
- **PAT-006**: Facade pattern to provide unified interface while maintaining backward compatibility

## 2. Implementation Steps

### Phase 1: Core Interfaces and Abstractions (Priority 1-3)

1. **Create persistence strategy interfaces**
   - Implement `ISemanticModelPersistenceStrategy` base interface
   - Define `ILocalDiskPersistenceStrategy`, `IAzureBlobPersistenceStrategy`, `ICosmosPersistenceStrategy`
   - Add factory interface for strategy selection

2. **Implement repository pattern abstraction**
   - Create `ISemanticModelRepository` interface
   - Implement base `SemanticModelRepository` class
   - Add strategy selection logic

3. **Configure dependency injection**
   - Register persistence strategies in DI container
   - Add configuration options for each strategy
   - Implement strategy factory with DI integration

**Phase 1 Status**: Completed on 2025-06-28 – core interfaces, repository abstraction, and DI registration implemented.

### Phase 2: Local Disk Strategy Enhancement (Priority 4-5)

Task 2.1: Enhance existing local disk persistence
  a. Define class `LocalDiskPersistenceStrategy : ILocalDiskPersistenceStrategy`:
     - Wrap calls to `SemanticModel.SaveModelAsync(DirectoryInfo)` and `SemanticModel.LoadModelAsync(DirectoryInfo)`.
     - Preserve existing public API signatures so that legacy callers are unaffected.
  b. Index file generation:
     - Produce `index.json` in model root listing entity categories (tables, views, storedprocedures) and relative file paths.
     - Use `System.Text.Json` with `WriteIndented = true` for readability.
  c. Validation and safety:
     - Apply `PathValidator` to sanitize `modelPath.FullName` and prevent directory traversal.
     - Enforce entity name length ≤128 via `EntityNameSanitizer`.
     - Log errors through `ILogger<LocalDiskPersistenceStrategy>` with structured context.
  d. Error handling and rollback:
     - Catch `IOException`, `UnauthorizedAccessException`, wrap with descriptive messages.
     - Write to a temp directory and use atomic rename/move to avoid partial writes.

Task 2.2: Add CRUD operations on disk
  a. Extend `ILocalDiskPersistenceStrategy` interface:
     - `Task<bool> ExistsAsync(DirectoryInfo modelPath)`
     - `Task<IEnumerable<string>> ListModelsAsync(DirectoryInfo rootPath)`
     - `Task DeleteModelAsync(DirectoryInfo modelPath)`
  b. Implement methods in `LocalDiskPersistenceStrategy`:
     - `ExistsAsync`: call `Directory.Exists`, validate path security.
     - `ListModelsAsync`: enumerate subfolders under `rootPath`, return model folder names.
     - `DeleteModelAsync`: acquire exclusive lock via `FileStream(FileShare.None)`, delete directory recursively, rollback on errors.
  c. Concurrency and atomicity:
     - Use `FileStream` locks and temp-to-final directory swaps for atomic operations.
     - Ensure file handles are released before moving or deleting.
  d. Testing requirements:
     - Unit tests against `Path.GetTempPath()`-based test directory, covering success and failure scenarios.
     - Regression tests to verify `SaveModelAsync`/`LoadModelAsync` still work on existing model artifacts.

### Phase 3: Cloud Persistence Strategies (Priority 6-7)

1. **Implement Azure Blob Storage strategy**
   - Create `AzureBlobPersistenceStrategy` class
   - Add Azure Storage SDK dependencies
   - Implement hierarchical blob naming and index blob management
   - Add connection string configuration and authentication

2. **Implement Cosmos DB strategy**
   - Create `CosmosPersistenceStrategy` class
   - Add Cosmos DB SDK dependencies
   - Implement hierarchical partition key structure
   - Add connection string configuration and authentication

### Phase 4: Advanced Features (Priority 8-11)

1. **Implement lazy loading mechanism**
   - Create lazy loading proxies for entities
   - Add deferred loading for Tables, Views, StoredProcedures collections
   - Implement memory optimization patterns
   - **ENSURE**: Lazy loading is opt-in and doesn't affect existing eager loading behavior

2. **Add dirty tracking system**
   - Implement change tracking for entities
   - Add `IChangeTracker` interface and implementation
   - Create selective persistence based on dirty state
   - **ENSURE**: Dirty tracking is optional and existing save operations continue to work

3. **Enhance security features**
   - Add path validation and sanitization (enhance existing validation, don't replace)
   - Implement input validation for all persistence operations
   - Add secure connection string handling
   - Implement JSON deserialization protection
   - **ENSURE**: Security enhancements are additive, not breaking

4. **Add performance optimizations**
   - Implement concurrent operation protection with semaphores
   - Add caching mechanisms for frequently accessed entities
   - Implement parallel processing for bulk operations
   - Add performance monitoring and metrics
   - **ENSURE**: Performance optimizations don't change existing API behavior

### Phase 5: Testing and Documentation (Priority 12-17)

1. **Implement comprehensive unit tests**
   - Test all persistence strategies independently
   - Test repository pattern abstraction
   - Test lazy loading and dirty tracking mechanisms
   - Test security validation and error handling

2. **Add integration tests**
   - Test end-to-end persistence workflows
   - Test concurrent operations and thread safety
   - Test performance benchmarks
   - Test Azure and Cosmos DB integration

3. **Update documentation and examples**
   - Update API documentation
   - Add usage examples for each persistence strategy
   - Create migration guide from existing implementation
   - Add troubleshooting guide

## 2.1. Backward Compatibility Strategy

**CRITICAL REQUIREMENT**: The application must remain fully functional after each phase with zero breaking changes to existing APIs.

### Phase-by-Phase Compatibility Guarantee

**Phase 1**: ✅ **SAFE** - Only adds new interfaces and abstractions. No existing code is modified.

- New interfaces are added but not yet used
- Existing `SemanticModel.SaveModelAsync()` and `LoadModelAsync()` continue to work unchanged
- DI registration is additive (new services registered alongside existing ones)

**Phase 2**: ✅ **SAFE** - Enhances local disk functionality without breaking existing behavior.

- `LocalDiskPersistenceStrategy` wraps existing `SemanticModel` methods internally
- Existing method signatures and behavior preserved
- New CRUD operations are additional capabilities, don't replace existing ones
- File format remains compatible (index document is optional enhancement)

**Phase 3**: ✅ **SAFE** - Adds new cloud persistence capabilities as separate strategies.

- Azure Blob and Cosmos DB are entirely new capabilities
- No changes to existing local disk persistence
- New strategies are isolated and don't affect existing code paths

**Phase 4**: ✅ **SAFE** - Advanced features are opt-in enhancements.

- Lazy loading is optional and doesn't change eager loading behavior
- Dirty tracking is additive and doesn't change existing save operations
- Security enhancements are additional validation layers, not replacements
- Performance optimizations don't change API behavior

**Phase 5**: ✅ **SAFE** - Testing and documentation don't affect runtime behavior.

- No code changes that could break existing functionality
- Testing validates that existing behavior is preserved

### Compatibility Testing Strategy

- **REG-TEST-001**: Regression tests for all existing `SemanticModel` operations after each phase
- **REG-TEST-002**: File format compatibility tests to ensure existing models can still be loaded
- **REG-TEST-003**: API signature verification tests to prevent breaking changes
- **REG-TEST-004**: End-to-end workflow tests using existing calling patterns

## 3. Alternatives

- **ALT-001**: Entity Framework Core as ORM - Rejected due to complexity overhead and requirement for multiple storage types including file-based storage
- **ALT-002**: Single persistence interface without strategy pattern - Rejected as it violates open/closed principle and makes testing difficult
- **ALT-003**: Separate repositories per entity type - Rejected as it increases complexity and doesn't align with aggregate root pattern
- **ALT-004**: Synchronous-only API - Rejected due to performance requirements for I/O operations
- **ALT-005**: In-memory caching with write-through - Rejected as it doesn't meet persistence durability requirements

## 4. Dependencies

- **DEP-001**: Azure.Storage.Blobs NuGet package for Azure Blob Storage strategy
- **DEP-002**: Microsoft.Azure.Cosmos NuGet package for Cosmos DB strategy
- **DEP-003**: Microsoft.Extensions.DependencyInjection for DI integration
- **DEP-004**: Microsoft.Extensions.Configuration for configuration management
- **DEP-005**: Microsoft.Extensions.Logging for structured logging
- **DEP-006**: System.Text.Json for JSON serialization (already available)
- **DEP-007**: Existing GenAIDBExplorer.Core project structure and interfaces

## 5. Files

### New Files to Create

- **FILE-001**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/ISemanticModelRepository.cs` - Repository interface
- **FILE-002**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/SemanticModelRepository.cs` - Repository implementation
- **FILE-003**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/ISemanticModelPersistenceStrategy.cs` - Base strategy interface
- **FILE-004**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/ILocalDiskPersistenceStrategy.cs` - Local disk strategy interface
- **FILE-005**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/LocalDiskPersistenceStrategy.cs` - Local disk implementation
- **FILE-006**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/IAzureBlobPersistenceStrategy.cs` - Azure Blob strategy interface
- **FILE-007**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/AzureBlobPersistenceStrategy.cs` - Azure Blob implementation
- **FILE-008**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/ICosmosPersistenceStrategy.cs` - Cosmos DB strategy interface
- **FILE-009**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/CosmosPersistenceStrategy.cs` - Cosmos DB implementation
- **FILE-010**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/IPersistenceStrategyFactory.cs` - Strategy factory interface
- **FILE-011**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/PersistenceStrategyFactory.cs` - Strategy factory implementation
- **FILE-012**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/SemanticModel/Lazy/ILazyLoadingProxy.cs` - Lazy loading interface
- **FILE-013**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/SemanticModel/Lazy/LazyLoadingProxy.cs` - Lazy loading implementation
- **FILE-014**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/SemanticModel/ChangeTracking/IChangeTracker.cs` - Change tracker interface
- **FILE-015**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/SemanticModel/ChangeTracking/ChangeTracker.cs` - Change tracker implementation
- **FILE-016**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Security/PathValidator.cs` - Path validation utilities
- **FILE-017**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Security/EntityNameSanitizer.cs` - Entity name sanitization

### Files to Modify

- **FILE-018**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/SemanticModel/SemanticModel.cs` - Add repository integration and lazy loading
- **FILE-019**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/SemanticModel/ISemanticModel.cs` - Update interface with repository methods
- **FILE-020**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticModelProviders/SemanticModelProvider.cs` - Integrate with repository pattern
- **FILE-021**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticModelProviders/ISemanticModelProvider.cs` - Update interface for repository
- **FILE-022**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/GenAIDBExplorer.Core.csproj` - Add new NuGet package references
- **FILE-023**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Extensions/HostBuilderExtensions.cs` - Register repository services

### Test Files to Create

- **FILE-024**: `src/Tests/Unit/GenAIDBExplorer.Core.Tests/Repository/SemanticModelRepositoryTests.cs`
- **FILE-025**: `src/Tests/Unit/GenAIDBExplorer.Core.Tests/Repository/LocalDiskPersistenceStrategyTests.cs`
- **FILE-026**: `src/Tests/Unit/GenAIDBExplorer.Core.Tests/Repository/AzureBlobPersistenceStrategyTests.cs`
- **FILE-027**: `src/Tests/Unit/GenAIDBExplorer.Core.Tests/Repository/CosmosPersistenceStrategyTests.cs`
- **FILE-028**: `src/Tests/Integration/GenAIDBExplorer.Core.Tests/Repository/RepositoryIntegrationTests.cs`

## 6. Testing

### Unit Tests

- **TEST-001**: Repository pattern abstraction unit tests with mocked persistence strategies
- **TEST-002**: Local disk persistence strategy unit tests with temporary directories
- **TEST-003**: Azure Blob Storage persistence strategy unit tests with Azure Storage Emulator
- **TEST-004**: Cosmos DB persistence strategy unit tests with Cosmos DB Emulator
- **TEST-005**: Lazy loading proxy unit tests with mock entities
- **TEST-006**: Change tracking unit tests with entity modifications
- **TEST-007**: Security validation unit tests with malicious inputs
- **TEST-008**: Performance optimization unit tests with large datasets

### Integration Tests

- **TEST-009**: End-to-end persistence workflow tests across all strategies
- **TEST-010**: Concurrent operation tests with multiple threads
- **TEST-011**: Performance benchmark tests with 1000+ entities
- **TEST-012**: Azure cloud integration tests with real Azure services
- **TEST-013**: Cosmos DB integration tests with real Cosmos DB instances
- **TEST-014**: Backward compatibility tests with existing local disk format
- **TEST-015**: Migration tests from current implementation to new repository pattern

### Performance Tests

- **TEST-016**: Memory usage tests for lazy loading (target: ≥70% reduction)
- **TEST-017**: Entity loading performance tests (target: ≤5s for 1000 entities)
- **TEST-018**: Concurrent operation throughput tests
- **TEST-019**: Large model serialization/deserialization tests
- **TEST-020**: Network latency tests for cloud persistence strategies

## 7. Risks & Assumptions

### Risks

- **RISK-001**: Breaking changes to existing local disk format may require migration scripts
- **RISK-002**: Azure and Cosmos DB dependencies increase deployment complexity
- **RISK-003**: Lazy loading implementation may introduce subtle bugs with entity relationships
- **RISK-004**: Performance overhead from repository pattern abstraction layer
- **RISK-005**: Cloud service authentication and connection string management complexity
- **RISK-006**: Potential memory leaks from lazy loading proxies if not properly disposed
- **RISK-007**: Race conditions in concurrent scenarios despite protection mechanisms

### Assumptions

- **ASSUMPTION-001**: Azure Storage and Cosmos DB SDKs are stable and compatible with .NET 9
- **ASSUMPTION-002**: Existing SemanticModel structure can accommodate lazy loading without major changes
- **ASSUMPTION-003**: JSON serialization performance is acceptable for large models
- **ASSUMPTION-004**: Development team has access to Azure and Cosmos DB for testing
- **ASSUMPTION-005**: Current entity relationships don't have circular dependencies that would complicate lazy loading
- **ASSUMPTION-006**: File system permissions allow atomic operations for local disk strategy
- **ASSUMPTION-007**: Network connectivity is reliable for cloud persistence strategies

## 8. Related Specifications / Further Reading

- [Data Semantic Model Repository Pattern Specification](../spec/data-semantic-model-repository.md)
- [Infrastructure Deployment Bicep AVM Specification](../spec/infrastructure-deployment-bicep-avm.md)
- [Microsoft .NET Application Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Repository Pattern Documentation](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Azure Blob Storage .NET SDK Documentation](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet)
- [Azure Cosmos DB .NET SDK Documentation](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/sql-api-sdk-dotnet-standard)
- [Entity Framework Core Change Tracking](https://docs.microsoft.com/en-us/ef/core/change-tracking/)
- [System.Text.Json Serialization Guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/framework/performance/performance-tips)
- [Azure Well-Architected Framework](https://docs.microsoft.com/en-us/azure/architecture/framework/)
