---
title: Project Model - Technical Documentation
component_path: src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/
version: 1.0
date_created: 2025-01-13
last_updated: 2025-01-13
owner: GenAI Database Explorer Core Team
tags: [component, model, configuration, validation, project-management, settings]
---

The Project Model component provides a comprehensive configuration and project management system for the GenAI Database Explorer. It handles project initialization, configuration loading, validation, and settings management across multiple persistence strategies and AI service configurations.

## 1. Component Overview

### Purpose/Responsibility

- OVR-001: Manages project lifecycle including initialization, configuration loading, and validation
- OVR-002: Provides a unified interface for accessing all project settings including database, AI services, and persistence configurations
- OVR-003: Ensures data integrity through comprehensive validation of configuration settings and cross-component dependencies

## 2. Architecture Section

- ARC-001: **Configuration Pattern** - Uses Microsoft.Extensions.Configuration for JSON-based settings management
- ARC-002: **Strategy Pattern** - Implements configurable persistence strategies (LocalDisk, AzureBlob, CosmosDB)
- ARC-003: **Validation Pattern** - Leverages Data Annotations with custom validation attributes for comprehensive settings validation
- ARC-004: **Dependency Injection** - Designed for DI container integration with ILogger and IProject interface
- ARC-005: **Resource Management** - Uses ResourceManager for localized error and log messages

### Component Structure and Dependencies Diagram

```mermaid
graph TD
    subgraph "Project Model Component"
        A[Project] --> B[ProjectSettings]
        A --> C[IProject Interface]
        B --> D[DatabaseSettings]
        B --> E[DataDictionarySettings]
        B --> F[SemanticModelSettings]
        B --> G[OpenAIServiceSettings]
        B --> H[SemanticModelRepositorySettings]
        
        H --> I[LocalDiskConfiguration]
        H --> J[AzureBlobStorageConfiguration]
        H --> K[CosmosDbConfiguration]
        
        G --> L[OpenAIServiceDefaultSettings]
        G --> M[OpenAIServiceChatCompletionSettings]
        G --> N[OpenAIServiceEmbeddingSettings]
        
        A --> O[ProjectUtils]
        A --> P[NotEmptyOrWhitespaceAttribute]
        A --> Q[RequiredOnPropertyValueAttribute]
    end

    subgraph "External Dependencies"
        R[Microsoft.Extensions.Configuration]
        S[Microsoft.Extensions.Logging]
        T[System.ComponentModel.DataAnnotations]
        U[System.Resources.ResourceManager]
        V[DirectoryInfo/FileSystem]
    end

    subgraph "Validation System"
        W[ValidationContext]
        X[Validator.ValidateObject]
        Y[Custom Validation Attributes]
    end

    A --> R
    A --> S
    A --> T
    A --> U
    A --> V
    P --> T
    Q --> T
    A --> W
    A --> X
    A --> Y

    classDiagram
        class Project {
            +ProjectSettings Settings
            +DirectoryInfo ProjectDirectory
            +InitializeProjectDirectory(DirectoryInfo): void
            +LoadProjectConfiguration(DirectoryInfo): void
            -InitializeSettings(): void
            -ValidateSettings(): void
            -ValidatePersistenceStrategyConfiguration(): void
        }
        
        class IProject {
            <<interface>>
            +ProjectSettings Settings
            +DirectoryInfo ProjectDirectory
            +InitializeProjectDirectory(DirectoryInfo): void
            +LoadProjectConfiguration(DirectoryInfo): void
        }
        
        class ProjectSettings {
            +Version SettingsVersion
            +DatabaseSettings Database
            +DataDictionarySettings DataDictionary
            +OpenAIServiceSettings OpenAIService
            +SemanticModelSettings SemanticModel
            +SemanticModelRepositorySettings SemanticModelRepository
        }
        
        class SemanticModelSettings {
            +string PersistenceStrategy
            +int MaxDegreeOfParallelism
        }
        
        Project ..|> IProject
        Project --> ProjectSettings
        ProjectSettings --> SemanticModelSettings
```

## 3. Interface Documentation

- INT-001: **IProject** interface provides the primary contract for project management operations
- INT-002: Configuration binding automatically maps JSON settings to strongly-typed objects
- INT-003: Validation events are logged through ILogger with localized messages

| Method/Property | Purpose | Parameters | Return Type | Usage Notes |
|-----------------|---------|------------|-------------|-------------|
| Settings | Access to all project configuration settings | None | ProjectSettings | Populated after LoadProjectConfiguration |
| ProjectDirectory | Current project working directory | None | DirectoryInfo | Set during initialization or configuration loading |
| InitializeProjectDirectory | Creates new project with default structure | DirectoryInfo projectDirectory | void | Throws if directory not empty |
| LoadProjectConfiguration | Loads and validates existing project settings | DirectoryInfo projectDirectory | void | Validates all settings and persistence strategy |

## 4. Implementation Details

- IMP-001: **Configuration Loading**: Uses ConfigurationBuilder to load settings.json with automatic binding to strongly-typed classes
- IMP-002: **Validation Strategy**: Multi-level validation including Data Annotations, custom attributes, and cross-component validation
- IMP-003: **Persistence Strategy Validation**: Dynamic validation based on selected persistence strategy (LocalDisk/AzureBlob/Cosmos)
- IMP-004: **Resource Management**: Localized error and log messages using embedded ResourceManager for internationalization support

### Key Algorithms

#### Settings Validation Process

1. Load configuration from settings.json
2. Bind configuration sections to settings objects
3. Validate each settings section using Data Annotations
4. Perform cross-component validation (persistence strategy consistency)
5. Log validation results and throw exceptions for failures

#### Project Initialization Process

1. Verify target directory is empty
2. Copy default project template from embedded resources
3. Set project directory reference

## 5. Usage Examples

### Basic Usage

```csharp
// Initialize project with dependency injection
var logger = serviceProvider.GetRequiredService<ILogger<Project>>();
var project = new Project(logger);

// Load existing project configuration
var projectDir = new DirectoryInfo(@"C:\MyProjects\DatabaseExplorer");
project.LoadProjectConfiguration(projectDir);

// Access configuration settings
var connectionString = project.Settings.Database.ConnectionString;
var persistenceStrategy = project.Settings.SemanticModel.PersistenceStrategy;
```

### Advanced Usage

```csharp
// Initialize new project from scratch
var project = new Project(logger);
var newProjectDir = new DirectoryInfo(@"C:\NewProject");

try
{
    project.InitializeProjectDirectory(newProjectDir);
    project.LoadProjectConfiguration(newProjectDir);
    
    // Modify settings programmatically if needed
    project.Settings.Database.MaxDegreeOfParallelism = 4;
    project.Settings.SemanticModel.PersistenceStrategy = "AzureBlob";
}
catch (InvalidOperationException ex)
{
    // Handle directory not empty error
    Console.WriteLine($"Directory initialization failed: {ex.Message}");
}
catch (ValidationException ex)
{
    // Handle configuration validation errors
    Console.WriteLine($"Configuration validation failed: {ex.Message}");
}
```

- USE-001: Always handle validation exceptions when loading project configurations
- USE-002: Use dependency injection to provide ILogger&lt;Project&gt; for proper logging
- USE-003: Check DirectoryInfo.Exists before calling InitializeProjectDirectory

## 6. Quality Attributes

### Security (QUA-001)

- **Input Validation**: All configuration properties use Data Annotations validation
- **File System Security**: Validates directory permissions and prevents path traversal
- **Configuration Security**: Sensitive settings (API keys) should use secure configuration providers

### Performance (QUA-002)

- **Configuration Caching**: Settings loaded once and cached in memory
- **Lazy Validation**: Validation only performed during configuration loading
- **Resource Efficiency**: Uses lightweight DirectoryInfo operations for file system access

### Reliability (QUA-003)

- **Exception Handling**: Comprehensive validation with specific exception types
- **Resource Cleanup**: Proper disposal of configuration builders and file handles
- **Fault Tolerance**: Graceful handling of missing or malformed configuration files

### Maintainability (QUA-004)

- **Separation of Concerns**: Clear separation between configuration, validation, and project management
- **Extensibility**: Easy to add new settings sections and validation rules
- **Testability**: Full unit test coverage with mock-friendly design using IProject interface

### Extensibility (QUA-005)

- **Settings Extension**: New settings classes can be added by extending ProjectSettings
- **Validation Extension**: Custom validation attributes can be created by inheriting ValidationAttribute
- **Persistence Strategy Extension**: New persistence strategies supported through configuration pattern

## 7. Reference Information

### Dependencies (REF-001)

- **Microsoft.Extensions.Configuration** (^8.0.0) - Configuration management and JSON binding
- **Microsoft.Extensions.Logging** (^8.0.0) - Structured logging with dependency injection
- **System.ComponentModel.DataAnnotations** (Built-in) - Validation attributes and validation context
- **System.Resources** (Built-in) - Localized resource management for messages

### Configuration Options Reference (REF-002)

#### Required Settings Structure

```json
{
  "SettingsVersion": "1.0.0",
  "Database": {
    "Name": "string (required)",
    "ConnectionString": "string (required)",
    "Description": "string (optional)",
    "Schema": "string (optional)",
    "MaxDegreeOfParallelism": 1,
    "NotUsedTables": ["regex_pattern"],
    "NotUsedColumns": ["regex_pattern"],
    "NotUsedViews": ["regex_pattern"],
    "NotUsedStoredProcedures": ["regex_pattern"]
  },
  "SemanticModel": {
    "PersistenceStrategy": "LocalDisk|AzureBlob|Cosmos (required)",
    "MaxDegreeOfParallelism": 1
  },
  "SemanticModelRepository": {
    "LocalDisk": { "Directory": "string" },
    "AzureBlobStorage": { 
      "AccountEndpoint": "string",
      "ContainerName": "string",
      "BlobPrefix": "string",
      "OperationTimeoutSeconds": 600,
      "MaxConcurrentOperations": 8
    },
    "CosmosDb": {
      "AccountEndpoint": "string",
      "DatabaseName": "string",
      "ModelsContainerName": "string",
      "EntitiesContainerName": "string",
      "ConsistencyLevel": "Strong|Session|Consistent|BoundedStaleness|Eventual"
    }
  }
}
```

### Testing Guidelines (REF-003)

- **Unit Testing**: Use MSTest with FluentAssertions and Moq for mock dependencies
- **Integration Testing**: Test complete configuration loading and validation scenarios
- **Test Structure**: Follow AAA pattern (Arrange, Act, Assert)
- **Mock Setup**: Mock ILogger&lt;Project&gt; for testing without side effects

### Troubleshooting (REF-004)

#### Common Issues and Solutions

| Error Message | Cause | Resolution |
|--------------|-------|------------|
| "ErrorProjectFolderNotEmpty" | Directory contains files during initialization | Clear directory or choose empty location |
| "AzureBlobStorage configuration is required..." | Missing persistence strategy config | Add required configuration section |
| "Invalid PersistenceStrategy" | Unsupported strategy value | Use LocalDisk, AzureBlob, or Cosmos |
| ValidationException on property | Missing required configuration | Check Data Annotations requirements |

### Related Documentation (REF-005)

- [Semantic Model Repository Documentation](semantic-model-repository-documentation.md)
- [Database Configuration Guide](../INSTALLATION.md)
- [Project Quick Start](../QUICKSTART.md)

### Change History (REF-006)

- **v1.0** (2025-01-13): Initial documentation covering all Project Model components
- **Future**: Consider adding support for additional persistence strategies and enhanced validation
