---
title: Adding .NET Aspire Support to GenAIDBExplorer
version: 1.0
date_created: 2025-07-18
last_updated: 2025-07-18
owner: GenAI Database Explorer Team
tags: [infrastructure, aspire, observability, telemetry, opentelemetry, azure-monitor, console-app]
---

# Adding .NET Aspire Support to GenAIDBExplorer

This specification defines requirements and implementation steps for adding .NET Aspire support to the GenAIDBExplorer solution. This will provide enhanced observability, telemetry, and operational capabilities to the existing console application and core library.

## 1. Purpose & Scope

**Purpose**: Implement .NET Aspire integration for GenAIDBExplorer to gain advanced observability, telemetry tracking, and cloud-native capabilities for improved monitoring, performance analysis, and debugging.

**Scope**: Integration of .NET Aspire components for the existing Console and Core applications, supporting both local development and cloud deployment scenarios.

**Audience**: Developers, DevOps engineers, and solution architects.

**Assumptions**:

- Target runtime is .NET 9
- Solution consists primarily of a console application and core library
- Telemetry requirements include both local development visibility and cloud production monitoring
- Application will be deployable to Azure with Application Insights integration

## 2. Definitions

- **AppHost**: A specialized project in .NET Aspire that orchestrates resources and services in a distributed application
- **ServiceDefaults**: A shared project containing configuration defaults for telemetry, health checks, and service discovery
- **OpenTelemetry**: An observability framework for cloud-native software, providing vendor-agnostic APIs, libraries, agents, and instrumentation
- **Observability**: The ability to measure a system's internal state by examining its outputs
- **OTLP** (OpenTelemetry Protocol): A vendor-agnostic protocol for transmitting telemetry data to a collector
- **Azure Monitor**: Azure's platform service providing observability across applications, infrastructure, and networks
- **Application Insights**: A component of Azure Monitor for application performance management
- **Azure.Monitor.OpenTelemetry.AspNetCore**: Microsoft's distribution of the OpenTelemetry SDK optimized for Azure
- **Health Checks**: Endpoints that report the operational health of an application

## 3. Requirements, Constraints & Guidelines

### Core Requirements

- **REQ-001**: Add .NET Aspire ServiceDefaults project to provide standardized telemetry configuration
- **REQ-002**: Add .NET Aspire AppHost project to orchestrate the console application
- **REQ-003**: Configure OpenTelemetry to capture metrics, traces, and logs for all operations
- **REQ-004**: Support both local development telemetry visualization and Azure Monitor integration
- **REQ-005**: Add health check endpoints for monitoring application operational status
- **REQ-006**: Ensure the console application can function as a standalone executable without AppHost requirements
- **REQ-007**: Support distributed tracing for LLM operations, SQL queries, and HTTP client requests
- **REQ-008**: Visualize telemetry data in the .NET Aspire dashboard
- **REQ-009**: Export telemetry to Azure Application Insights in cloud environments

### Security Requirements

- **SEC-001**: Store connection strings securely using environment variables or secret management
- **SEC-002**: Ensure PII (Personally Identifiable Information) is not captured in telemetry
- **SEC-003**: Implement appropriate sampling rates to control telemetry data volume
- **SEC-004**: Restrict health endpoint access in production environments

### Constraints

- **CON-001**: Must support both interactive execution and background service execution
- **CON-002**: Must not introduce breaking changes to the existing command line interface
- **CON-003**: Must maintain existing functionality across all commands
- **CON-004**: Telemetry overhead must not exceed 5% of total application performance
- **CON-005**: Must support graceful degradation when telemetry backends are unavailable

### Guidelines

- **GUD-001**: Use standard .NET Aspire patterns and extension methods
- **GUD-002**: Follow Microsoft recommended approaches for console applications
- **GUD-003**: Use asynchronous operations for telemetry export
- **GUD-004**: Apply consistent naming conventions for metrics and traces
- **GUD-005**: Use [HostApplicationBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.hostapplicationbuilder) pattern for application setup

### Patterns

- **PAT-001**: Use ServiceDefaults pattern with AddServiceDefaults() extension method
- **PAT-002**: Use Azure Monitor OpenTelemetry Distro for Application Insights integration
- **PAT-003**: Use built-in health checks with standard endpoint configuration
- **PAT-004**: Use Activity and ActivitySource for custom instrumentation
- **PAT-005**: Use DistributedApplicationBuilder for AppHost orchestration

## 4. Interfaces & Data Contracts

### Project Structure

```text
src/
  GenAIDBExplorer/
    GenAIDBExplorer.sln
    GenAIDBExplorer.AppHost/            # NEW: Orchestration project
      GenAIDBExplorer.AppHost.csproj
      Program.cs
    GenAIDBExplorer.ServiceDefaults/    # NEW: Shared configuration project
      GenAIDBExplorer.ServiceDefaults.csproj
      Extensions.cs
    GenAIDBExplorer.Console/            # UPDATED: Modified to use ServiceDefaults
      GenAIDBExplorer.Console.csproj
      Program.cs
    GenAIDBExplorer.Core/               # UNCHANGED: No modification needed
      GenAIDBExplorer.Core.csproj
```

### ServiceDefaults Extensions

```csharp
// Extensions.cs in ServiceDefaults project
public static class Extensions
{
    // Adds service defaults including OpenTelemetry, health checks, and service discovery
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        
        builder.AddDefaultHealthChecks();
        
        // Configure service discovery (if needed)
        builder.Services.AddServiceDiscovery();
        
        return builder;
    }

    // Configures OpenTelemetry for metrics, traces, and logs
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("GenAIDBExplorer");
                metrics.AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource("GenAIDBExplorer");
                tracing.AddHttpClientInstrumentation();
                tracing.AddSqlClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }
    
    // Configures exporters based on environment
    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Azure Monitor integration
        if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor();
        }

        return builder;
    }

    // Adds default health checks
    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }
}
```

### AppHost Configuration

```csharp
// Program.cs in AppHost project
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add the console app as a project
var console = builder.AddProject<Projects.GenAIDBExplorer_Console>("genaidbexplorer");

builder.Build().Run();
```

### Modified Console Program

```csharp
// Program.cs in Console project
using System.CommandLine;
using Microsoft.Extensions.Hosting;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Console.Extensions;

namespace GenAIDBExplorer.Console;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Create the root command with a description
        var rootCommand = new RootCommand("GenAI Database Explorer console application");

        // Build the host with ServiceDefaults
        var host = Host.CreateApplicationBuilder(args)
            .AddServiceDefaults() // Add .NET Aspire service defaults
            .ConfigureHost(args)  // Add existing configuration
            .Build();

        // Set up commands
        rootCommand.Subcommands.Add(InitProjectCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(DataDictionaryCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(EnrichModelCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(ExportModelCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(ExtractModelCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(QueryModelCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(ShowObjectCommandHandler.SetupCommand(host));

        try
        {
            await rootCommand.Parse(args).InvokeAsync();
            return 0;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
```

### Package References

```xml
<!-- ServiceDefaults project -->
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.0.0" />
  <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.0.0" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.12.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.12.0" />
  <PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.3.0" />
</ItemGroup>

<!-- AppHost project -->
<ItemGroup>
  <ProjectReference Include="..\GenAIDBExplorer.ServiceDefaults\GenAIDBExplorer.ServiceDefaults.csproj" />
  <ProjectReference Include="..\GenAIDBExplorer.Console\GenAIDBExplorer.Console.csproj" />
</ItemGroup>
```

## 5. Acceptance Criteria

- **AC-001**: ServiceDefaults project successfully compiles and integrates with the solution
- **AC-002**: AppHost project successfully orchestrates the console application
- **AC-003**: Console application runs both with and without the AppHost
- **AC-004**: Console application emits OpenTelemetry data when running
- **AC-005**: Aspire dashboard displays telemetry data when running through AppHost
- **AC-006**: Telemetry data includes metrics, traces, and logs for core operations
- **AC-007**: Health endpoints correctly report application health status
- **AC-008**: Azure Application Insights integration works when connection string provided
- **AC-009**: Telemetry overhead does not exceed 5% of application performance
- **AC-010**: Application gracefully handles scenarios when telemetry is unavailable
- **AC-011**: Key operations have proper distributed tracing across components

## 6. Test Automation Strategy

**Frameworks**:

- MSTest for unit testing
- FluentAssertions for assertions
- Moq for mocking dependencies

**Test Approach**:

- Unit tests for telemetry instrumentation
- Integration tests for telemetry collection
- Performance tests for overhead measurement

**Test Coverage Areas**:

- OpenTelemetry configuration and integration
- AppHost orchestration and service discovery
- Health check functionality
- Azure Monitor integration

**Methodology**:

- Test telemetry collection using OpenTelemetry in-memory exporters
- Test health checks using direct HTTP calls
- Compare performance with and without telemetry enabled

## 7. Rationale & Context

.NET Aspire provides cloud-native capabilities that greatly enhance the operational visibility of .NET applications. By adding .NET Aspire support to GenAIDBExplorer, we gain:

1. **Enhanced Observability**: OpenTelemetry integration provides standardized metrics, traces, and logs that can be visualized and analyzed in various tools.

2. **Local Development Experience**: The .NET Aspire dashboard provides real-time visibility into application behavior during development, enabling faster debugging and performance optimization.

3. **Cloud Integration**: Seamless integration with Azure Application Insights enables production monitoring without additional code changes.

4. **Health Monitoring**: Standardized health checks provide operational status information for monitoring systems.

5. **Future Extensibility**: The .NET Aspire framework provides a foundation for adding additional cloud-native capabilities in the future, such as service discovery and resilience patterns.

The console application structure presents unique challenges for .NET Aspire integration, since Aspire was primarily designed for web applications. However, by using the Custom ServiceDefaults approach described in Microsoft's documentation, we can adapt Aspire's capabilities for console applications while maintaining backward compatibility.

## 8. Dependencies & External Integrations

### External Systems

- **EXT-001**: Azure Application Insights - Cloud-based monitoring service for telemetry storage and analysis
- **EXT-002**: OpenTelemetry Collector (optional) - Data collection service for aggregating telemetry

### Technology Platform Dependencies

- **PLT-001**: .NET 9.0 Runtime - Required for execution of application
- **PLT-002**: Docker - Required for local development with .NET Aspire dashboard

### Core Dependencies

- **DEP-001**: Microsoft.Extensions.Hosting - Foundation for hosting the application
- **DEP-002**: OpenTelemetry SDK - Foundation for telemetry collection
- **DEP-003**: Azure.Monitor.OpenTelemetry.AspNetCore - Integration with Azure Monitor
- **DEP-004**: Microsoft.Extensions.ServiceDiscovery - Service discovery capabilities

## 9. Examples & Edge Cases

### Example: Running with .NET Aspire Dashboard

```bash
# From solution root directory
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.AppHost/GenAIDBExplorer.AppHost.csproj
```

### Example: Running Standalone

```bash
# From solution root directory
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/GenAIDBExplorer.Console.csproj
```

### Example: Configuring Azure Monitor

```bash
# Set the environment variable for Application Insights
$env:APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://regionname.in.applicationinsights.azure.com/"

# Run the application
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/GenAIDBExplorer.Console.csproj
```

### Edge Case: Handling Telemetry Unavailability

The application should continue functioning even when telemetry services are unavailable. This is handled by graceful degradation in the OpenTelemetry configuration. If telemetry endpoints are unreachable, the application will log warning messages but continue operating.

### Edge Case: High-Throughput Scenarios

For operations that generate high volumes of telemetry, sampling should be implemented to prevent overwhelming the telemetry backend. This can be configured through the OpenTelemetry SDK's sampling options.

## 10. Validation Criteria

- OpenTelemetry metrics, traces, and logs are properly collected and exported
- Health endpoints correctly respond with the application's operational status
- Telemetry data is visible in the .NET Aspire dashboard
- Telemetry data is correctly exported to Azure Application Insights when configured
- Console application maintains full functionality with .NET Aspire integration
- All CLI commands continue to function as expected
- Performance overhead from telemetry is within acceptable limits (<5%)

## 11. Related Specifications / Further Reading

- [.NET Aspire overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [Adding .NET Aspire to an existing app](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/add-aspire-existing-app)
- [.NET Aspire service defaults](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults)
- [.NET Aspire telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry)
- [Azure Monitor OpenTelemetry](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable)
- [.NET observability with OpenTelemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel)
