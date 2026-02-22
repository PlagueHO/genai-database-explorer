# SqlConnectionProvider - Technical Documentation

A specialized database connection provider that creates and manages SQL Server database connections with support for both traditional SQL authentication and modern Microsoft Entra ID (Azure AD) authentication methods.

## 1. Component Overview

### Purpose/Responsibility

- OVR-001: Primary responsibility is to establish and provide live SQL Server database connections using project-configured connection strings and authentication methods
- OVR-002: Scope includes connection string management, authentication mode selection (SQL vs Entra ID), connection lifecycle management, error handling, and logging
- OVR-003: System context: Core data access component in the GenAI Database Explorer that serves as the foundation for all SQL Server database operations, used by DatabaseConnectionManager and consumed by higher-level data services

## 2. Architecture Section

- ARC-001: Implements Factory Pattern for connection creation, Provider Pattern for abstracted database connectivity, and Strategy Pattern for authentication method selection
- ARC-002: Internal dependencies include IProject for configuration access, ILogger for structured logging, and ResourceManager for localized messages; External dependencies are Microsoft.Data.SqlClient for SQL connectivity and System.Resources for message resources
- ARC-003: Direct integration with DatabaseConnectionManager (consumer), indirect usage by SqlQueryExecutor and SchemaRepository through connection manager
- ARC-004: Follows .NET dependency injection patterns with primary constructor injection and implements IDatabaseConnectionProvider interface contract
- ARC-005: Component supports both traditional SQL Server authentication and modern cloud-native authentication flows through Microsoft Entra ID integration

### Component Structure and Dependencies Diagram

```mermaid
graph TD
    subgraph "SqlConnectionProvider System"
        SCP[SqlConnectionProvider] --> ICP[IDatabaseConnectionProvider]
        SCP --> IP[IProject]
        SCP --> IL[ILogger]
        SCP --> RM1[ResourceManager - LogMessages]
        SCP --> RM2[ResourceManager - ErrorMessages]
        
        IP --> PS[ProjectSettings]
        PS --> DS[DatabaseSettings]
        DS --> DAT[DatabaseAuthenticationType]
    end

    subgraph "External Dependencies"
        MSDC[Microsoft.Data.SqlClient]
        SQLCSB[SqlConnectionStringBuilder]
        SQLAM[SqlAuthenticationMethod]
        SR[System.Resources]
        ML[Microsoft.Extensions.Logging]
        
        SCP --> MSDC
        SCP --> SQLCSB
        SCP --> SQLAM
        RM1 --> SR
        RM2 --> SR
        IL --> ML
    end

    subgraph "Consumers"
        DCM[DatabaseConnectionManager]
        SQE[SqlQueryExecutor]
        REPO[SchemaRepository]
        
        DCM --> SCP
        SQE --> DCM
        REPO --> DCM
    end

    subgraph "Authentication Flow"
        DAT --> |SqlAuthentication| SQLAUTH[SQL Server Auth]
        DAT --> |EntraIdAuthentication| ENTRAID[Entra ID Auth]
        
        SQLAUTH --> SQLCONN[SqlConnection]
        ENTRAID --> ADDEFAULT[ActiveDirectoryDefault]
        ADDEFAULT --> SQLCONN
    end

    classDiagram
        class SqlConnectionProvider {
            -IProject _project
            -ILogger _logger
            -ResourceManager _resourceManagerLogMessages
            -ResourceManager _resourceManagerErrorMessages
            +ConnectAsync(): Task~SqlConnection~
        }
        
        class IDatabaseConnectionProvider {
            <<interface>>
            +ConnectAsync(): Task~SqlConnection~
        }
        
        class DatabaseSettings {
            +string ConnectionString
            +DatabaseAuthenticationType AuthenticationType
            +string Name
            +string Description
            +string Schema
            +int MaxDegreeOfParallelism
        }
        
        class DatabaseAuthenticationType {
            <<enumeration>>
            SqlAuthentication
            EntraIdAuthentication
        }
        
        SqlConnectionProvider ..|> IDatabaseConnectionProvider
        SqlConnectionProvider --> DatabaseSettings
        DatabaseSettings --> DatabaseAuthenticationType
```

## 3. Interface Documentation

- INT-001: Implements IDatabaseConnectionProvider interface providing standardized database connection abstraction
- INT-002: Primary method ConnectAsync() returns Task\<SqlConnection\> with comprehensive error handling and logging
- INT-003: No events or callbacks - follows synchronous factory pattern with async connection establishment

| Method/Property | Purpose | Parameters | Return Type | Usage Notes |
|-----------------|---------|------------|-------------|-------------|
| ConnectAsync() | Creates and opens SQL database connection | None | Task\<SqlConnection\> | Returns connection in Open state; handles both SQL and Entra ID auth |
| Constructor | Dependency injection setup | IProject project, ILogger logger | N/A | Primary constructor pattern with required dependencies |

## 4. Implementation Details

- IMP-001: Main implementation class SqlConnectionProvider handles connection creation, authentication mode selection, connection string manipulation, and comprehensive error handling
- IMP-002: Configuration requirements include valid connection string in project settings, optional authentication type selection (defaults to SQL authentication), and proper logging configuration
- IMP-003: Key algorithms include connection string validation, authentication-specific connection string modification (removing conflicting properties for Entra ID), SqlConnectionStringBuilder usage for safe manipulation, and DefaultAzureCredential chain for cloud authentication
- IMP-004: Performance characteristics include connection pooling enabled by default for efficient reuse, minimal overhead for connection string manipulation, structured logging for observability, and proper resource disposal on connection failures

## 5. Usage Examples

### Basic Usage

```csharp
// Dependency injection setup
services.AddScoped<IDatabaseConnectionProvider, SqlConnectionProvider>();

// Basic usage with SQL authentication
var connectionProvider = serviceProvider.GetService<IDatabaseConnectionProvider>();
using var connection = await connectionProvider.ConnectAsync();
// Connection is ready for database operations
```

### Advanced Usage

```csharp
// Project configuration for Entra ID authentication
var projectSettings = new ProjectSettings
{
    Database = new DatabaseSettings
    {
        ConnectionString = "Server=myserver.database.windows.net;Database=mydatabase;",
        AuthenticationType = DatabaseAuthenticationType.EntraIdAuthentication,
        Name = "Production Database",
        Description = "Main application database with customer data"
    }
};

// The provider automatically handles authentication mode selection
var connectionProvider = new SqlConnectionProvider(project, logger);
using var connection = await connectionProvider.ConnectAsync();

// For SQL authentication, include credentials in connection string
var sqlAuthSettings = new DatabaseSettings
{
    ConnectionString = "Server=localhost;Database=testdb;User ID=sa;Password=password123;",
    AuthenticationType = DatabaseAuthenticationType.SqlAuthentication
};
```

- USE-001: Basic usage involves dependency injection registration and simple ConnectAsync() calls
- USE-002: Advanced configuration supports both authentication methods through DatabaseSettings configuration
- USE-003: Best practices include proper connection disposal using 'using' statements, structured logging configuration, and connection string security considerations

## 6. Quality Attributes

- QUA-001: Security features include secure connection string handling with automatic removal of conflicting authentication properties, support for Microsoft Entra ID with DefaultAzureCredential chain (managed identity, Visual Studio, Azure CLI), no credential caching or exposure in logs, and proper credential validation
- QUA-002: Performance characteristics include default connection pooling for efficient reuse, minimal allocation during connection string manipulation, async/await patterns for non-blocking operations, and efficient resource cleanup on failures
- QUA-003: Reliability includes comprehensive exception handling with specific SqlException and general Exception catching, proper connection disposal on errors, structured logging for troubleshooting, and graceful degradation with meaningful error messages
- QUA-004: Maintainability features include clean separation of concerns with single responsibility, comprehensive XML documentation, unit test coverage with mocking support, and resource-based localized error messages
- QUA-005: Extensibility supports easy addition of new authentication methods through enum extension, pluggable logging through ILogger abstraction, configurable connection string modification, and interface-based design for alternative implementations

## 7. Reference Information

- REF-001: Dependencies include Microsoft.Data.SqlClient 6.1.1+ for SQL connectivity, Microsoft.Extensions.Logging for structured logging, GenAIDBExplorer.Core.Models.Project for configuration models, and System.Resources for localized messages
- REF-002: Configuration options include ConnectionString (required database connection string), AuthenticationType (SqlAuthentication or EntraIdAuthentication), Name (friendly database name), Description (database purpose description), Schema (optional schema filter), and MaxDegreeOfParallelism (concurrent query limit)
- REF-003: Testing guidelines include mock IProject and ILogger dependencies, test both authentication types with appropriate connection strings, verify exception handling for invalid configurations, and validate proper connection disposal
- REF-004: Common troubleshooting includes "Missing database connection string" for empty connection strings, SqlException for database connectivity issues, authentication failures for Entra ID misconfiguration, and timeout issues for network connectivity problems
- REF-005: Related documentation includes IDatabaseConnectionProvider interface definition, DatabaseSettings configuration model, DatabaseConnectionManager usage patterns, and Azure authentication setup guides
- REF-006: Change history includes initial implementation with SQL authentication support, addition of Entra ID authentication with DefaultAzureCredential, enum-based authentication type selection, and comprehensive error handling improvements
