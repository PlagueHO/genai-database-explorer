---
title: GenAI Database Explorer Project Structure and Configuration Management Specification
version: 1.0
date_created: 2024-12-30
last_updated: 2024-12-30
owner: GenAI Database Explorer Development Team
tags: [project, architecture, configuration, structure, genai, database, explorer]
---

This specification defines the standardized project structure, configuration management, and dependency injection patterns for GenAI Database Explorer projects.

## 1. Purpose & Scope

This specification establishes the standard project structure, configuration management patterns, and service integration guidelines for GenAI Database Explorer projects. It defines how projects are initialized, configured, and managed across different deployment scenarios including CLI applications, web APIs, and Azure Functions.

**Intended Audience**: Developers, architects, and DevOps engineers working with GenAI Database Explorer applications.

**Assumptions**:

- Target runtime is .NET 9 with C# 12+ language features
- Projects follow Microsoft dependency injection patterns
- Configuration uses JSON-based settings with strong typing
- Azure deployment scenarios are primary focus

## 2. Definitions

- **GenAI DB Explorer**: Generative AI Database Explorer - tool for database semantic modeling and AI-powered querying
- **Project Directory**: Root folder containing settings.json and associated project artifacts
- **Semantic Model**: AI-generated representation of database schema, relationships, and business context
- **Settings Schema**: Strongly-typed configuration classes with validation attributes
- **Persistence Strategy**: Pattern for storing semantic models (File, Azure Blob, CosmosDB)
- **DI Container**: Dependency Injection container managing service lifetimes and dependencies
- **Command Handler**: Service responsible for executing specific CLI commands with proper dependency resolution

## 3. Requirements, Constraints & Guidelines

### Project Structure Requirements

- **REQ-001**: Project directories MUST contain a valid settings.json file conforming to ProjectSettings schema
- **REQ-002**: All configuration sections (Database, DataDictionary, SemanticModel, OpenAIService) MUST be present and valid
- **REQ-003**: Project initialization MUST use DefaultProject template copy pattern
- **REQ-004**: Settings validation MUST occur during project loading with comprehensive error reporting
- **REQ-005**: Project directory validation MUST prevent overwriting existing non-empty directories

### Configuration Management Requirements

- **REQ-006**: Configuration MUST use strongly-typed classes with DataAnnotation validation
- **REQ-007**: Settings MUST support both development and production Azure scenarios
- **REQ-008**: Configuration binding MUST use Microsoft.Extensions.Configuration patterns
- **REQ-009**: Sensitive settings (API keys) MUST support Azure Key Vault integration
- **REQ-010**: Settings versioning MUST be tracked and validated for compatibility
- **REQ-016**: SemanticModel section MUST support persistence strategy selection (LocalDisk, AzureBlob, Cosmos)
- **REQ-017**: Configuration MUST include strategy-specific settings sections for AzureBlob and CosmosDb
- **REQ-018**: Each persistence strategy MUST have appropriate validation attributes for required fields
- **REQ-019**: Azure-based strategies MUST support DefaultAzureCredential and connection string authentication

### Dependency Injection Requirements

- **REQ-011**: All services MUST be registered through proper DI container configuration
- **REQ-012**: Command handlers MUST receive dependencies via constructor injection
- **REQ-013**: Service lifetimes MUST be appropriate (Singleton for stateless, Scoped for request-bound)
- **REQ-014**: Circular dependencies MUST be avoided through proper interface design
- **REQ-015**: Service registration MUST support multiple deployment targets (Console, WebAPI, Functions)

### Security Requirements

- **SEC-001**: Connection strings MUST support secure credential management
- **SEC-002**: API keys MUST NOT be stored in plain text in production environments
- **SEC-003**: File system access MUST be validated and sandboxed to project directories
- **SEC-004**: Input validation MUST be applied to all user-provided configuration values

### Performance Constraints

- **CON-001**: Project loading MUST complete within 2 seconds for typical configurations
- **CON-002**: Settings validation MUST not exceed 500ms for complex schemas
- **CON-003**: Memory footprint for project configuration MUST not exceed 50MB baseline
- **CON-004**: Lazy loading MUST be used for expensive service dependencies

### Design Guidelines

- **GUD-001**: Follow SOLID principles in all service and interface designs
- **GUD-002**: Use immutable objects for configuration settings where possible
- **GUD-003**: Implement comprehensive logging for configuration and validation operations
- **GUD-004**: Provide clear error messages with actionable guidance for configuration issues
- **GUD-005**: Support both imperative and declarative configuration approaches

### Implementation Patterns

- **PAT-001**: Use Options pattern for complex configuration objects
- **PAT-002**: Implement Builder pattern for optional configuration scenarios
- **PAT-003**: Apply Factory pattern for strategy-based service instantiation
- **PAT-004**: Use Repository pattern for data persistence abstractions
- **PAT-005**: Follow Command pattern for CLI operations with proper error handling

## 4. Interfaces & Data Contracts

### Core Project Interface

```csharp
public interface IProject
{
    DirectoryInfo ProjectDirectory { get; }
    ProjectSettings Settings { get; }
    
    void InitializeProjectDirectory(DirectoryInfo projectDirectory);
    void LoadProjectConfiguration(DirectoryInfo projectDirectory);
}
```

### Configuration Schema

```csharp
public class ProjectSettings
{
    [Required, NotEmptyOrWhitespace]
    public Version? SettingsVersion { get; set; }
    
    public required DatabaseSettings Database { get; set; }
    public required DataDictionarySettings DataDictionary { get; set; }
    public required OpenAIServiceSettings OpenAIService { get; set; }
    public required SemanticModelSettings SemanticModel { get; set; }
    public required SemanticModelRepositorySettings SemanticModelRepository { get; set; }
}

public class SemanticModelSettings
{
    public const string PropertyName = "SemanticModel";
    
    // Default persistence strategy is LocalDisk
    [Required, NotEmptyOrWhitespace]
    public string PersistenceStrategy { get; set; } = "LocalDisk";
    
    public int MaxDegreeOfParallelism { get; set; } = 1;
}

public class SemanticModelRepositorySettings
{
    public const string PropertyName = "SemanticModelRepository";
    
    public LocalDiskConfiguration? LocalDisk { get; set; }
    public AzureBlobConfiguration? AzureBlob { get; set; }
    public CosmosDbConfiguration? CosmosDb { get; set; }
    
    public LazyLoadingConfiguration LazyLoading { get; set; } = new();
    public CachingConfiguration Caching { get; set; } = new();
    public ChangeTrackingConfiguration ChangeTracking { get; set; } = new();
    public PerformanceMonitoringConfiguration PerformanceMonitoring { get; set; } = new();
    
    [Range(1, 50)]
    public int MaxConcurrentOperations { get; set; } = 10;
}

public class LocalDiskConfiguration
{
    // If Directory is not set, it will automatically default to "SemanticModel"
    [Required, NotEmptyOrWhitespace]
    public string Directory { get; set; } = "SemanticModel";
}

public class AzureBlobConfiguration
{
    [Required, Url]
    public required string AccountEndpoint { get; set; }
    
    [Required, RegularExpression(@"^[a-z0-9]([a-z0-9\-]{1,61}[a-z0-9])?$")]
    public string ContainerName { get; set; } = "semantic-models";
    
    public string? BlobPrefix { get; set; }
    
    [Range(30, 3600)]
    public int OperationTimeoutSeconds { get; set; } = 300;
    
    [Range(1, 16)]
    public int MaxConcurrentOperations { get; set; } = 4;
    
    public bool UseCustomerManagedKeys { get; set; } = false;
    
    [Url]
    public string? CustomerManagedKeyUrl { get; set; }
}

public class CosmosDbConfiguration
{
    [Required, Url]
    public required string AccountEndpoint { get; set; }
    
    [Required, RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9_\-\.]{0,253}[a-zA-Z0-9])?$")]
    public string DatabaseName { get; set; } = "SemanticModels";
    
    [Required, RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9_\-\.]{0,253}[a-zA-Z0-9])?$")]
    public string ModelsContainerName { get; set; } = "Models";
    
    [Required, RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9_\-\.]{0,253}[a-zA-Z0-9])?$")]
    public string EntitiesContainerName { get; set; } = "ModelEntities";
    
    [Required]
    public string ModelsPartitionKeyPath { get; set; } = "/modelName";
    
    [Required]
    public string EntitiesPartitionKeyPath { get; set; } = "/modelName";
    
    [Range(400, 1000000)]
    public int? DatabaseThroughput { get; set; } = 400;
    
    [Range(30, 3600)]
    public int OperationTimeoutSeconds { get; set; } = 300;
    
    [Range(1, 16)]
    public int MaxConcurrentOperations { get; set; } = 4;
    
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;
    
    public CosmosConsistencyLevel ConsistencyLevel { get; set; } = CosmosConsistencyLevel.Session;
}

public class LazyLoadingConfiguration
{
    public bool Enabled { get; set; } = true;
}

public class CachingConfiguration
{
    public bool Enabled { get; set; } = true;
    
    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 30;
}

public class ChangeTrackingConfiguration
{
    public bool Enabled { get; set; } = true;
}

public class PerformanceMonitoringConfiguration
{
    public bool Enabled { get; set; } = true;
    public bool DetailedTiming { get; set; } = false;
    public bool MetricsEnabled { get; set; } = true;
}

public enum CosmosConsistencyLevel
{
    Eventual,
    ConsistentPrefix,
    Session,
    BoundedStaleness,
    Strong
}
```

### Settings.json Structure

```json
{
    "SettingsVersion": "1.0.0",
    "Database": {
        "Name": "Sample Database",
        "Description": "Database description for AI context",
        "ConnectionString": "Server=.;Database=Sample;Integrated Security=true;",
        "Schema": ["dbo"],
        "Tables": [],
        "StoredProcedures": []
    },
    "DataDictionary": {
        "Strategy": "File",
        "Directory": "DataDictionary"
    },
    "SemanticModel": {
        "PersistenceStrategy": "LocalDisk",
        "MaxDegreeOfParallelism": 1,
        "Directory": "SemanticModel"
    },
    "SemanticModelRepository": {
        "LocalDisk": {
            "Directory": "SemanticModel"
        },
        "AzureBlob": {
            "AccountEndpoint": "https://mystorageaccount.blob.core.windows.net",
            "ContainerName": "semantic-models",
            "BlobPrefix": "",
            "OperationTimeoutSeconds": 300,
            "MaxConcurrentOperations": 4,
            "UseCustomerManagedKeys": false,
            "CustomerManagedKeyUrl": ""
        },
        "CosmosDb": {
            "AccountEndpoint": "https://mycosmosaccount.documents.azure.com:443/",
            "DatabaseName": "SemanticModels",
            "ModelsContainerName": "Models",
            "EntitiesContainerName": "ModelEntities",
            "ModelsPartitionKeyPath": "/modelName",
            "EntitiesPartitionKeyPath": "/modelName",
            "DatabaseThroughput": 400,
            "OperationTimeoutSeconds": 300,
            "MaxConcurrentOperations": 4,
            "MaxRetryAttempts": 3,
            "ConsistencyLevel": "Session"
        },
        "LazyLoading": {
            "Enabled": true
        },
        "Caching": {
            "Enabled": true,
            "ExpirationMinutes": 30
        },
        "ChangeTracking": {
            "Enabled": true
        },
        "PerformanceMonitoring": {
            "Enabled": true,
            "DetailedTiming": false,
            "MetricsEnabled": true
        },
        "MaxConcurrentOperations": 10
    },
    "OpenAIService": {
        "Default": {
            "ServiceType": "AzureOpenAI",
            "AzureOpenAIKey": "",
            "AzureOpenAIEndpoint": ""
        },
        "ChatCompletion": {
            "AzureOpenAIDeploymentId": "gpt-4o"
        },
        "ChatCompletionStructured": {
            "AzureOpenAIDeploymentId": "gpt-4o"
        },
        "Embedding": {
            "AzureOpenAIDeploymentId": "text-embedding-3-large"
        }
    }
}
```

## 5. Acceptance Criteria

- **AC-001**: Given a valid project directory path, When InitializeProjectDirectory is called, Then a complete project structure is created with valid settings.json
- **AC-002**: Given an existing non-empty directory, When InitializeProjectDirectory is called, Then an InvalidOperationException is thrown with clear error message
- **AC-003**: Given a project directory with valid settings.json, When LoadProjectConfiguration is called, Then all configuration sections are bound and validated successfully
- **AC-004**: Given invalid configuration values, When settings validation occurs, Then specific DataAnnotation errors are reported with field-level detail
- **AC-005**: Given multiple deployment scenarios, When services are registered, Then appropriate lifetimes and dependencies are configured for each target
- **AC-006**: Given Azure Key Vault configuration, When sensitive settings are accessed, Then credentials are retrieved securely without exposing plain text
- **AC-007**: Given a command handler execution, When dependencies are resolved, Then all required services are available through constructor injection
- **AC-008**: Given lazy loading requirements, When expensive services are registered, Then instantiation is deferred until actual usage
- **AC-009**: Given SemanticModel section with PersistenceStrategy "LocalDisk", When project configuration is loaded, Then LocalDiskPersistenceStrategy is selected and LocalDisk.Directory setting is applied
- **AC-010**: Given SemanticModel section with PersistenceStrategy "AzureBlob", When project configuration is loaded, Then AzureBlobPersistenceStrategy is selected and AzureBlob settings are validated
- **AC-011**: Given SemanticModel section with PersistenceStrategy "CosmosDb", When project configuration is loaded, Then CosmosPersistenceStrategy is selected and CosmosDb settings are validated
- **AC-012**: Given missing AzureBlob configuration, When PersistenceStrategy is "AzureBlob", Then configuration validation fails with descriptive error message
- **AC-013**: Given missing CosmosDb configuration, When PersistenceStrategy is "CosmosDb", Then configuration validation fails with descriptive error message
- **AC-014**: Given invalid Azure endpoints or container names, When configuration validation occurs, Then DataAnnotation validation reports specific field errors
- **AC-015**: Given production Azure deployment, When DefaultAzureCredential is used, Then authentication succeeds without requiring connection strings in configuration

## 6. Test Automation Strategy

### Test Levels

- **Unit Tests**: Service registration, configuration binding, validation logic, project initialization
- **Integration Tests**: Full DI container resolution, file system operations, configuration loading
- **End-to-End Tests**: Complete project lifecycle from initialization through configuration validation

### Testing Frameworks

- **MSTest**: Primary testing framework for .NET projects
- **FluentAssertions**: Assertion library for readable test expectations
- **Moq**: Mocking framework for dependency isolation
- **Microsoft.Extensions.DependencyInjection**: For testing service registration patterns

### Test Data Management

- **Temporary Directories**: Use TestContext or temporary folder creation for file system tests
- **Configuration Fixtures**: Predefined settings.json templates for various test scenarios
- **Mock Services**: Comprehensive mocking of external dependencies (Azure services, file system)

### CI/CD Integration

- **GitHub Actions**: Automated test execution on pull requests and main branch commits
- **Test Results**: Integration with GitHub test reporting and coverage analysis
- **Build Validation**: Ensure all tests pass before deployment to any environment

### Coverage Requirements

- **Minimum Coverage**: 85% code coverage for service registration and configuration logic
- **Critical Path Coverage**: 100% coverage for project initialization and settings validation
- **Exception Handling**: Full coverage of error scenarios and edge cases

### Performance Testing

- **Configuration Loading**: Validate settings loading performance under various file sizes
- **Service Resolution**: Measure DI container resolution time for complex dependency graphs
- **Memory Usage**: Profile memory consumption during project lifecycle operations

## 7. Rationale & Context

### Design Decision Rationale

**Strong Typing for Configuration**: Using strongly-typed configuration classes with DataAnnotations provides compile-time safety, IntelliSense support, and runtime validation. This reduces configuration errors and improves developer experience.

**Dependency Injection Pattern**: Following Microsoft's DI patterns ensures consistency with .NET ecosystem standards, improves testability, and supports multiple deployment scenarios (Console, WebAPI, Azure Functions).

**Project Template Approach**: Using DefaultProject template copying provides consistent project structure, reduces setup complexity, and ensures all required files are present from initialization.

**Lazy Loading for Expensive Services**: Implementing lazy loading for services like Azure Blob Storage prevents unnecessary service instantiation during development and testing scenarios.

**Separation of Configuration Concerns**: Dividing configuration into logical sections (Database, OpenAIService, etc.) improves maintainability and allows for targeted validation and updates.

### Business Context

The GenAI Database Explorer is designed to democratize database exploration and querying through AI-powered semantic modeling. The project structure must support diverse deployment scenarios while maintaining consistency and reliability across different environments.

## 8. Dependencies & External Integrations

### External Systems

- **EXT-001**: SQL Server Database - Primary data source for semantic model extraction
- **EXT-002**: Azure OpenAI Service - AI model hosting for chat completion and embeddings
- **EXT-003**: OpenAI API - Alternative AI service provider for development scenarios

### Third-Party Services

- **SVC-001**: Azure Key Vault - Secure credential storage with high availability requirements
- **SVC-002**: Azure Blob Storage - Semantic model persistence with geo-redundancy (when AzureBlob strategy is selected)
- **SVC-003**: Azure AI Search - Vector storage and search capabilities for semantic operations
- **SVC-004**: Azure Cosmos DB - Document-based semantic model persistence with global distribution (when Cosmos strategy is selected)

### Infrastructure Dependencies

- **INF-001**: .NET 9 Runtime - Target platform with modern C# language support
- **INF-002**: File System Access - Local storage for development and file-based persistence
- **INF-003**: Network Connectivity - Internet access for Azure services and AI model APIs

### Data Dependencies

- **DAT-001**: Database Metadata - Schema information extraction from target databases
- **DAT-002**: Data Dictionary Files - Business context and semantic enrichment data
- **DAT-003**: Semantic Model Cache - Persistent storage of generated AI semantic models

### Technology Platform Dependencies

- **PLT-001**: Microsoft.Extensions.DependencyInjection - Service registration and resolution
- **PLT-002**: Microsoft.Extensions.Configuration - JSON configuration binding and validation
- **PLT-003**: System.CommandLine - CLI framework for command handling and option parsing
- **PLT-004**: Semantic Kernel SDK - AI orchestration and model integration

### Compliance Dependencies

- **COM-001**: Azure Security Standards - Compliance with Azure security and governance requirements
- **COM-002**: Data Privacy Regulations - Ensure configuration management respects data privacy laws

## 9. Examples & Edge Cases

### Basic Project Initialization

```csharp
// Initialize new project
var project = serviceProvider.GetRequiredService<IProject>();
var projectPath = new DirectoryInfo(@"C:\MyGenAIProject");
project.InitializeProjectDirectory(projectPath);

// Load existing project
project.LoadProjectConfiguration(projectPath);
var connectionString = project.Settings.Database.ConnectionString;
```

### Service Registration Pattern

```csharp
// Program.cs - Console Application
services.AddSingleton<IProject, Project>();
services.AddScoped<ISemanticModelRepository, SemanticModelRepository>();
services.AddTransient<ICommandHandler<InitProjectCommandHandlerOptions>, InitProjectCommandHandler>();

// Register persistence strategies
services.AddTransient<ILocalDiskPersistenceStrategy, LocalDiskPersistenceStrategy>();
services.AddTransient<IAzureBlobPersistenceStrategy, AzureBlobPersistenceStrategy>();
services.AddTransient<ICosmosPersistenceStrategy, CosmosPersistenceStrategy>();
services.AddSingleton<IPersistenceStrategyFactory, PersistenceStrategyFactory>();

// Configuration binding
services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.PropertyName));
services.Configure<OpenAIServiceSettings>(configuration.GetSection(OpenAIServiceSettings.PropertyName));
services.Configure<SemanticModelSettings>(configuration.GetSection(SemanticModelSettings.PropertyName));
services.Configure<LocalDiskConfiguration>(configuration.GetSection("SemanticModelRepository:LocalDisk"));
services.Configure<AzureBlobConfiguration>(configuration.GetSection("SemanticModelRepository:AzureBlob"));
services.Configure<CosmosDbConfiguration>(configuration.GetSection("SemanticModelRepository:CosmosDb"));
```

### Persistence Strategy Configuration Examples

**Local Disk Strategy (Development)**:

```json
{
    "SemanticModel": {
        "PersistenceStrategy": "LocalDisk",
        "MaxDegreeOfParallelism": 1
    },
    "SemanticModelRepository": {
        "LocalDisk": {
            "Directory": "SemanticModel"
        }
    }
}
```

**Azure Blob Storage Strategy (Cloud)**:

```json
{
    "SemanticModel": {
        "PersistenceStrategy": "AzureBlob",
        "MaxDegreeOfParallelism": 4
    },
    "SemanticModelRepository": {
        "AzureBlob": {
            "AccountEndpoint": "https://mystorageaccount.blob.core.windows.net",
            "ContainerName": "semantic-models",
            "BlobPrefix": "models",
            "OperationTimeoutSeconds": 300,
            "MaxConcurrentOperations": 4
        }
    }
}
```

**Azure Cosmos DB Strategy (Global Scale)**:

```json
{
    "SemanticModel": {
        "PersistenceStrategy": "CosmosDb",
        "MaxDegreeOfParallelism": 8
    },
    "SemanticModelRepository": {
        "CosmosDb": {
            "AccountEndpoint": "https://mycosmosaccount.documents.azure.com:443/",
            "DatabaseName": "SemanticModels",
            "ModelsContainerName": "Models",
            "EntitiesContainerName": "ModelEntities",
            "ConsistencyLevel": "Session",
            "DatabaseThroughput": 1000,
            "MaxConcurrentOperations": 8
        }
    }
}
```

### Edge Cases

**Empty Directory Validation**:

```csharp
// Should succeed
project.InitializeProjectDirectory(new DirectoryInfo(@"C:\EmptyFolder"));

// Should throw InvalidOperationException
project.InitializeProjectDirectory(new DirectoryInfo(@"C:\NonEmptyFolder"));
```

**Missing Configuration Sections**:

```json
{
    "SettingsVersion": "1.0.0",
    "Database": {
        // Missing required ConnectionString - should fail validation
        "Name": "Test Database"
    }
    // Missing OpenAIService section - should fail binding
}
```

**Invalid Persistence Strategy Configuration**:

```json
{
    "SemanticModel": {
        "PersistenceStrategy": "AzureBlob"
        // Missing AzureBlob configuration - should fail validation
    }
}
```

**Azure Service Unavailability**:

```csharp
// Lazy loading should prevent failures during registration
services.AddTransient<IAzureBlobPersistenceStrategy>(provider => 
{
    // Only instantiated when actually used
    return new AzureBlobPersistenceStrategy(/* Azure dependencies */);
});
```

**Persistence Strategy Selection**:

```csharp
// Strategy factory resolves based on configuration
var factory = serviceProvider.GetRequiredService<IPersistenceStrategyFactory>();
var strategy = factory.GetStrategy("AzureBlob"); // Returns IAzureBlobPersistenceStrategy
var cosmosStrategy = factory.GetStrategy("CosmosDb"); // Returns ICosmosPersistenceStrategy
var localStrategy = factory.GetStrategy(); // Defaults to LocalDisk
```

## 10. Validation Criteria

### Configuration Validation

- All required fields in ProjectSettings must be populated and valid
- Database connection string must be syntactically correct and testable
- OpenAI service configuration must match selected service type (OpenAI vs AzureOpenAI)
- File paths in settings must be accessible and writable
- Version compatibility must be verified between settings schema and application
- SemanticModel PersistenceStrategy must be one of: LocalDisk, AzureBlob, Cosmos
- When PersistenceStrategy is "AzureBlob", AzureBlob configuration section must be present and valid
- When PersistenceStrategy is "CosmosDb", CosmosDb configuration section must be present and valid
- Azure endpoints must be valid URLs with appropriate domain patterns
- Container and database names must meet Azure naming requirements
- Timeout and concurrency settings must be within acceptable ranges

### Service Registration Validation

- All required services must be registered with appropriate lifetimes
- Circular dependencies must be detected and reported during container validation
- Command handlers must receive all required dependencies through constructor injection
- Service resolution must complete successfully for all registered types

### Runtime Validation

- Project initialization must create all required files and directories
- Configuration loading must complete without exceptions for valid settings
- Service instantiation must occur only when required (lazy loading validation)
- Error handling must provide actionable feedback for configuration issues

## 11. Related Specifications / Further Reading

- [Semantic Model Storage Specification](./spec-data-semantic-model-repository.md)
- [Infrastructure Deployment Specification](./spec-infrastructure-deployment-bicep-avm.md)
- [Azure Application Insights OpenTelemetry Specification](./spec-monitoring-azure-application-insights-opentelemetry.md)
- [Microsoft Dependency Injection Guidelines](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [.NET Configuration Patterns](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault Configuration Provider](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
