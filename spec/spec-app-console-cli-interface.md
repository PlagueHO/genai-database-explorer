---
title: GenAI Database Explorer Console Application CLI Interface Specification
version: 1.0
date_created: 2025-01-24
owner: GenAI Database Explorer Team
tags: [app, cli, console, interface, commands]
---

# Introduction

This specification defines the command-line interface (CLI) for the GenAI Database Explorer Console application. The console application provides a comprehensive set of commands for managing database semantic models, from project initialization through model extraction, enrichment, and querying using natural language processing capabilities.

## 1. Purpose & Scope

This specification defines the functional requirements, command structure, parameter validation, and behavior expectations for the GenAI Database Explorer console application CLI. The specification covers all available commands, their parameters, validation rules, and expected outputs.

**Target Audience**: Developers, DevOps engineers, database administrators, and AI practitioners working with database semantic modeling.

**Scope**: This specification covers the CLI interface only and does not include implementation details of the underlying Core library components.

## 2. Definitions

- **CLI**: Command Line Interface
- **GAIDBEXP**: GenAI Database Explorer (tool abbreviation)
- **Semantic Model**: AI-enhanced database schema representation for natural language querying
- **Data Dictionary**: Metadata files that provide additional context for database objects
- **Project**: A workspace containing configuration, semantic models, and related artifacts
- **Command Handler**: Implementation class that processes specific CLI commands
- **System.CommandLine**: .NET library used for CLI command parsing and validation

## 3. Requirements, Constraints & Guidelines

### Functional Requirements

- **REQ-001**: The console application SHALL provide seven primary commands: init-project, extract-model, data-dictionary, enrich-model, show-object, query-model, and export-model
- **REQ-002**: All commands SHALL validate required parameters before execution
- **REQ-003**: The application SHALL provide meaningful error messages for invalid parameters or execution failures
- **REQ-004**: Commands SHALL support both short and long parameter names where applicable
- **REQ-005**: The application SHALL use structured logging for operational visibility
- **REQ-006**: Commands SHALL provide help text and usage examples

### Security Requirements

- **SEC-001**: Database connection strings SHALL NOT be logged in plain text
- **SEC-002**: The application SHALL validate file paths to prevent directory traversal attacks
- **SEC-003**: AI service credentials SHALL be loaded from secure configuration sources

### Performance Constraints

- **CON-001**: Command execution SHALL complete within reasonable timeframes (< 5 minutes for typical operations)
- **CON-002**: The application SHALL handle database connection failures gracefully with retry logic
- **CON-003**: Memory usage SHALL be optimized for large database schemas

### Implementation Guidelines

- **GUD-001**: Use System.CommandLine library for command parsing and validation
- **GUD-002**: Implement dependency injection for all command handlers
- **GUD-003**: Follow async/await patterns for all I/O operations
- **GUD-004**: Use Microsoft.Extensions.Hosting for application lifecycle management
- **GUD-005**: Implement proper resource disposal for database connections

### Design Patterns

- **PAT-001**: Each command SHALL be implemented as a separate CommandHandler class
- **PAT-002**: Command options SHALL be defined in dedicated Options classes
- **PAT-003**: Use the Command pattern for encapsulating command logic
- **PAT-004**: Implement factory pattern for command setup and registration

## 4. Interfaces & Data Contracts

### Command Structure

All commands follow the pattern:

```bash
gaidbexp <command> [options]
```

### Base Command Interface

```csharp
public interface ICommandHandler<T> where T : ICommandHandlerOptions
{
    Task<int> ExecuteAsync(T options);
}

public interface ICommandHandlerOptions
{
    string ProjectPath { get; set; }
}
```

### Command Definitions

#### 4.1 init-project Command

**Purpose**: Initialize a new GenAI Database Explorer project

**Syntax**:

```bash
gaidbexp init-project --project <path>
```

**Parameters**:

- `--project`, `-p` (required): Project directory path
  - Type: string
  - Validation: Must be a valid directory path
  - Description: Target directory for project initialization
  - Behavior: Creates the directory if it does not exist; throws exception if directory exists and is not empty

**Exit Codes**:

- 0: Success
- 1: Error (invalid parameters, file system errors, directory exists and is not empty)

#### 4.2 extract-model Command

**Purpose**: Extract semantic model from database schema

**Syntax**:

```bash
gaidbexp extract-model --project <path> [--skipTables] [--skipViews] [--skipStoredProcedures]
```

**Parameters**:

- `--project`, `-p` (required): Project directory path
- `--skipTables` (optional): Skip table extraction
- `--skipViews` (optional): Skip view extraction
- `--skipStoredProcedures` (optional): Skip stored procedure extraction

#### 4.3 data-dictionary Command

**Purpose**: Apply data dictionary files to semantic model

**Syntax**:

```bash
gaidbexp data-dictionary --project <path> --sourcePathPattern <pattern> [options]
```

**Parameters**:

- `--project`, `-p` (required): Project directory path
- `--sourcePathPattern`, `-s` (required): Glob pattern for data dictionary files
- `--objectType` (optional): Filter by object type (table, view, storedprocedure)
- `--schemaName` (optional): Filter by schema name
- `--objectName` (optional): Filter by object name
- `--show` (optional): Display processed entity details

#### 4.4 enrich-model Command

**Purpose**: Enrich semantic model using Generative AI

**Syntax**:

```bash
gaidbexp enrich-model --project <path> [options]
```

**Parameters**:

- `--project`, `-p` (required): Project directory path
- `--skipTables` (optional): Skip table enrichment
- `--skipViews` (optional): Skip view enrichment
- `--skipStoredProcedures` (optional): Skip stored procedure enrichment
- `--objectType` (optional): Target specific object type
- `--schemaName` (optional): Target specific schema
- `--objectName` (optional): Target specific object
- `--show` (optional): Display enriched entity details

#### 4.5 show-object Command

**Purpose**: Display database object details from semantic model

**Syntax**:

```bash
gaidbexp show-object <objectType> --project <path> --schemaName <name> --name <name>
```

**Parameters**:

- `objectType` (required): Object type (table, view, storedprocedure)
- `--project`, `-p` (required): Project directory path
- `--schemaName`, `-s` (required): Schema name
- `--name`, `-n` (required): Object name

#### 4.6 query-model Command

**Purpose**: Natural language querying of semantic model

**Syntax**:

```bash
gaidbexp query-model --project <path> [--question <question>]
```

**Parameters**:

- `--project`, `-p` (required): Project directory path
- `--question`, `-q` (optional): Natural language question to ask about the semantic model
  - Type: string
  - Description: The question to query against the semantic model
  - Behavior: If provided, executes the single query and exits; if not provided, enters interactive mode for multiple queries

#### 4.7 export-model Command

**Purpose**: Export semantic model to external formats

**Syntax**:

```bash
gaidbexp export-model --project <path> [options]
```

**Parameters**:

- `--project`, `-p` (required): Project directory path
- `--outputPath`, `-o` (optional): Output file path (default: exported_model.md)
- `--fileType`, `-f` (optional): Output format (default: markdown)
- `--splitFiles` (optional): Create separate files per entity

## 5. Acceptance Criteria

### Command Registration and Discovery

- **AC-001**: Given the application starts, When no command is specified, Then help text SHALL be displayed with all available commands
- **AC-002**: Given an invalid command is provided, When the application executes, Then an error message SHALL be displayed with available commands
- **AC-003**: Given the --help flag is used with any command, When executed, Then command-specific help SHALL be displayed

### Parameter Validation

- **AC-004**: Given a required parameter is missing, When a command executes, Then a validation error SHALL be returned with exit code 1
- **AC-005**: Given an invalid file path is provided, When a command executes, Then a file system error SHALL be returned
- **AC-006**: Given valid parameters are provided, When a command executes, Then the command SHALL proceed to execution

### Project Initialization

- **AC-007**: Given a non-existent project directory is specified, When init-project executes, Then the directory SHALL be created and initialized with default project structure
- **AC-008**: Given an existing empty project directory is specified, When init-project executes, Then the directory SHALL be initialized with default project structure
- **AC-009**: Given an existing non-empty project directory is specified, When init-project executes, Then an InvalidOperationException SHALL be thrown with exit code 1

### Error Handling

- **AC-010**: Given a database connection failure occurs, When extracting models, Then a clear error message SHALL be displayed
- **AC-011**: Given insufficient permissions exist, When writing files, Then a permission error SHALL be reported
- **AC-012**: Given an unexpected exception occurs, When any command executes, Then the error SHALL be logged and a user-friendly message displayed

### Configuration Management

- **AC-013**: Given a project directory exists, When commands execute, Then project settings SHALL be loaded from settings.json
- **AC-014**: Given environment variables are set, When the application starts, Then configuration SHALL be loaded from multiple sources
- **AC-015**: Given AI service configuration is missing, When AI-dependent commands execute, Then a configuration error SHALL be reported

## 6. Test Automation Strategy

### Test Levels

- **Unit Tests**: Command handler logic, parameter validation, option binding
- **Integration Tests**: End-to-end command execution with test databases
- **System Tests**: Full CLI workflow testing with real project scenarios

### Test Frameworks

- **MSTest**: Primary test framework for .NET unit tests
- **FluentAssertions**: Assertion library for readable test expectations
- **Moq**: Mocking framework for isolating dependencies
- **PowerShell Pester**: Integration testing for CLI commands

### Test Data Management

- **Test Databases**: Lightweight SQLite databases for integration tests
- **Mock Data**: Sample semantic models and data dictionaries
- **Test Projects**: Temporary project directories with known configurations

### CI/CD Integration

- **GitHub Actions**: Automated test execution on pull requests
- **Test Coverage**: Minimum 80% code coverage for command handlers
- **Cross-Platform**: Test execution on Windows, Linux, and macOS

### Performance Testing

- **Load Testing**: Large database schema processing
- **Memory Profiling**: Resource usage monitoring during execution
- **Timeout Testing**: Ensure commands complete within expected timeframes

## 7. Rationale & Context

### Technology Choices

**System.CommandLine**: Selected for robust CLI parsing, validation, and help generation capabilities. Provides type-safe parameter binding and modern .NET CLI patterns.

**Dependency Injection**: Enables testable command handlers and proper separation of concerns. Facilitates mock injection for unit testing.

**Async/Await**: Essential for database operations and AI service calls that involve network I/O and potentially long-running operations.

### Command Design Philosophy

Commands are designed to be composable and support both interactive and automation scenarios. Each command has a single responsibility and can be used independently or as part of larger workflows.

### Error Handling Strategy

The application follows fail-fast principles with meaningful error messages. All errors are logged with appropriate severity levels while presenting user-friendly messages at the console.

## 8. Dependencies & External Integrations

### External Systems

- **EXT-001**: SQL Server Database - Primary target for semantic model extraction
- **EXT-002**: Azure OpenAI Service - Generative AI model enrichment capabilities
- **EXT-003**: OpenAI API - Alternative AI service provider

### Third-Party Services

- **SVC-001**: AI Language Models - Natural language processing and SQL generation
- **SVC-002**: Azure AI Services - Optional cloud-based AI processing

### Infrastructure Dependencies

- **INF-001**: .NET 10 Runtime - Target framework for application execution
- **INF-002**: File System Access - Project file management and configuration storage
- **INF-003**: Network Connectivity - Database and AI service communication

### Data Dependencies

- **DAT-001**: Database Schema Access - Read permissions for metadata extraction
- **DAT-002**: Project Configuration Files - settings.json and related metadata
- **DAT-003**: Data Dictionary Files - JSON/YAML files with object descriptions

### Technology Platform Dependencies

- **PLT-001**: System.CommandLine Library - CLI framework and command parsing
- **PLT-002**: Microsoft.Extensions.Hosting - Application lifecycle and dependency injection
- **PLT-003**: Microsoft.Extensions.Logging - Structured logging infrastructure

### Compliance Dependencies

- **COM-001**: Data Privacy Regulations - Ensure no sensitive data in logs or exports
- **COM-002**: Security Standards - Secure credential handling and API access

## 9. Examples & Edge Cases

### Basic Project Workflow

```bash
# Initialize new project
gaidbexp init-project --project /home/user/myproject

# Extract database schema
gaidbexp extract-model --project /home/user/myproject

# Apply data dictionary
gaidbexp data-dictionary --project /home/user/myproject \
  --sourcePathPattern "/home/user/dictionaries/*.json"

# Enrich with AI
gaidbexp enrich-model --project /home/user/myproject

# Interactive querying
gaidbexp query-model --project /home/user/myproject

# Single question query
gaidbexp query-model --project /home/user/myproject \
  --question "Show me all customers from California"

# Export results
gaidbexp export-model --project /home/user/myproject \
  --outputPath /home/user/output.md --splitFiles
```

### Edge Cases

```bash
# Initialize project in non-existent directory (creates directory)
gaidbexp init-project --project /home/user/newproject
# Expected: Directory created and initialized successfully

# Initialize project in existing empty directory
gaidbexp init-project --project /home/user/emptyproject
# Expected: Directory initialized successfully

# Initialize project in existing non-empty directory
gaidbexp init-project --project /home/user/existingproject
# Expected: InvalidOperationException with exit code 1

# Handle missing project directory for other commands
gaidbexp extract-model --project /nonexistent/path
# Expected: Error message with exit code 1

# Skip all object types (no-op scenario)
gaidbexp extract-model --project /valid/path \
  --skipTables --skipViews --skipStoredProcedures
# Expected: Warning message, no extraction performed

# Invalid object type
gaidbexp show-object invalidtype --project /valid/path \
  --schemaName dbo --name tablename
# Expected: Parameter validation error

# Large database timeout handling
gaidbexp extract-model --project /project/large-db
# Expected: Progress indicators, timeout protection

# Query with question parameter (single execution)
gaidbexp query-model --project /valid/path \
  --question "What are the top 10 selling products?"
# Expected: Execute query and exit with results

# Query without question parameter (interactive mode)
gaidbexp query-model --project /valid/path
# Expected: Enter interactive mode for multiple queries

# Empty question parameter
gaidbexp query-model --project /valid/path --question ""
# Expected: Parameter validation error or enter interactive mode
```

### Configuration Examples

```json
// settings.json
{
  "DatabaseConnection": {
    "ConnectionString": "Server=localhost;Database=test;Integrated Security=true"
  },
  "AzureOpenAI": {
    "Endpoint": "https://example.openai.azure.com/",
    "DeploymentName": "gpt-4"
  }
}
```

## 10. Validation Criteria

### Functional Validation

- All seven commands execute successfully with valid parameters
- Parameter validation prevents invalid inputs from causing runtime errors
- Help text is accurate and provides sufficient usage guidance
- Error messages are clear and actionable

### Performance Validation

- Command startup time is under 2 seconds
- Database schema extraction completes within 5 minutes for typical databases
- Memory usage remains stable during long-running operations
- AI enrichment operations provide progress feedback

### Security Validation

- No credentials or sensitive data appear in logs
- File path validation prevents directory traversal attacks
- Configuration loading follows secure practices
- AI service communication uses encrypted channels

### Usability Validation

- Commands follow consistent naming and parameter conventions
- Interactive mode provides intuitive user experience
- Output formatting is readable and useful
- Documentation matches actual command behavior

## 11. Related Specifications / Further Reading

- [GenAI Database Explorer Core Library Specification](spec-app-core-library.md)
- [Semantic Model Storage Specification](../docs/technical/SEMANTIC_MODEL_STORAGE.md)
- [Project Structure Documentation](../docs/technical/SEMANTIC_MODEL_PROJECT_STRUCTURE.md)
- [System.CommandLine Documentation](https://docs.microsoft.com/en-us/dotnet/standard/commandline/)
- [.NET Generic Host Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host)
