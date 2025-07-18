---
goal: Add .NET Aspire Support to GenAIDBExplorer
version: 1.0
date_created: 2025-07-18
last_updated: 2025-07-18
owner: GenAI Database Explorer Team
status: 'Planned'
tags: ['infrastructure', 'aspire', 'observability', 'telemetry', 'opentelemetry', 'azure-monitor', 'console-app']
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This implementation plan outlines the phased approach to integrate .NET Aspire support into the GenAIDBExplorer solution to provide enhanced observability, telemetry, and operational capabilities. Each phase is designed to build incrementally on the previous phase while maintaining a working application state at all times.

## 1. Requirements & Constraints

- **REQ-001**: Add .NET Aspire ServiceDefaults project to provide standardized telemetry configuration
- **REQ-002**: Add .NET Aspire AppHost project to orchestrate the console application
- **REQ-003**: Configure OpenTelemetry to capture metrics, traces, and logs for all operations
- **REQ-004**: Support both local development telemetry visualization and Azure Monitor integration
- **REQ-005**: Add health check endpoints for monitoring application operational status
- **REQ-006**: Ensure the console application can function as a standalone executable without AppHost requirements
- **REQ-007**: Support distributed tracing for LLM operations, SQL queries, and HTTP client requests
- **REQ-008**: Visualize telemetry data in the .NET Aspire dashboard
- **REQ-009**: Export telemetry to Azure Application Insights in cloud environments
- **SEC-001**: Store connection strings securely using environment variables or secret management
- **SEC-002**: Ensure PII (Personally Identifiable Information) is not captured in telemetry
- **CON-001**: Must support both interactive execution and background service execution
- **CON-002**: Must not introduce breaking changes to the existing command line interface
- **CON-003**: Must maintain existing functionality across all commands
- **CON-004**: Telemetry overhead must not exceed 5% of total application performance
- **CON-005**: Must support graceful degradation when telemetry backends are unavailable
- **PAT-001**: Use ServiceDefaults pattern with AddServiceDefaults() extension method
- **PAT-002**: Use Azure Monitor OpenTelemetry Distro for Application Insights integration

## 2. Implementation Steps

### Phase 1: Create ServiceDefaults Project

- GOAL-001: Add ServiceDefaults project with OpenTelemetry configuration and ensure the Console application still works without any breaking changes

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-001 | Create GenAIDBExplorer.ServiceDefaults project | | |
| TASK-002 | Configure ServiceDefaults project file with required packages | | |
| TASK-003 | Create Extensions.cs with baseline OpenTelemetry configuration | | |
| TASK-004 | Implement AddServiceDefaults and ConfigureOpenTelemetry methods | | |
| TASK-005 | Implement AddOpenTelemetryExporters method for OTLP and Azure Monitor | | |
| TASK-006 | Add baseline health checks implementation | | |
| TASK-007 | Add reference to ServiceDefaults project in GenAIDBExplorer.Console | | |
| TASK-008 | Update Program.cs to use CreateApplicationBuilder and AddServiceDefaults | | |
| TASK-009 | Verify console application runs without errors | | |
| TASK-010 | Create basic tests for ServiceDefaults methods | | |

### Phase 2: Add Custom Activity Sources and Meters

- GOAL-002: Implement custom telemetry instrumentation for GenAIDBExplorer components

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-011 | Create TelemetryConstants class to define source and meter names | | |
| TASK-012 | Define ActivitySource constants for Repository, LLM, Database, and Core operations | | |
| TASK-013 | Define Meter constants for GenAIDBExplorer metrics | | |
| TASK-014 | Extend ServiceDefaults to register custom activity sources | | |
| TASK-015 | Extend ServiceDefaults to register custom meters | | |
| TASK-016 | Update OpenTelemetry configuration to add SqlClientInstrumentation | | |
| TASK-017 | Add configuration to ensure PII is not captured in telemetry | | |
| TASK-018 | Add basic instrumentation to SemanticModelRepository for distributed tracing | | |
| TASK-019 | Add basic instrumentation to LLM operations for distributed tracing | | |
| TASK-020 | Verify telemetry is emitted using in-memory exporter | | |

### Phase 3: Create AppHost Project

- GOAL-003: Add AppHost project to orchestrate Console application while maintaining standalone functionality

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-021 | Create GenAIDBExplorer.AppHost project | | |
| TASK-022 | Configure AppHost project file with required packages | | |
| TASK-023 | Add reference to GenAIDBExplorer.Console project | | |
| TASK-024 | Implement basic Program.cs with DistributedApplication configuration | | |
| TASK-025 | Configure Console application resource in AppHost | | |
| TASK-026 | Configure environment variable passing for connection strings | | |
| TASK-027 | Create launchSettings.json for AppHost project | | |
| TASK-028 | Verify application orchestration through AppHost | | |
| TASK-029 | Create AppHost integration tests | | |

### Phase 4: Advanced Telemetry Configuration

- GOAL-004: Enhance telemetry configuration with sampling, resilience, and Azure Monitor integration

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-030 | Implement trace sampling configuration for high-volume scenarios | | |
| TASK-031 | Add resilient telemetry export with retry policies | | |
| TASK-032 | Add graceful degradation for telemetry failures | | |
| TASK-033 | Update Azure Monitor integration with resource attributes | | |
| TASK-034 | Implement telemetry context propagation across operation boundaries | | |
| TASK-035 | Add application lifecycle telemetry hooks | | |
| TASK-036 | Configure default dimensions for metrics | | |
| TASK-037 | Add telemetry processors to filter sensitive information | | |
| TASK-038 | Create utility methods for custom metric tracking | | |
| TASK-039 | Update documentation with telemetry configuration examples | | |

### Phase 5: Enhanced Health Checks and Testing

- GOAL-005: Expand health checks and ensure comprehensive testing coverage

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-040 | Add custom health checks for SemanticModelRepository | | |
| TASK-041 | Add custom health checks for LLM service availability | | |
| TASK-042 | Add custom health checks for database connections | | |
| TASK-043 | Configure health check publishing for Azure Application Insights | | |
| TASK-044 | Add health check endpoints to AppHost configuration | | |
| TASK-045 | Implement health check security for production environments | | |
| TASK-046 | Create tests for custom health checks | | |
| TASK-047 | Create performance tests to verify telemetry overhead | | |
| TASK-048 | Create integration tests for Azure Monitor export | | |
| TASK-049 | Create documentation for monitoring and telemetry | | |

## 3. Alternatives

- **ALT-001**: Manual OpenTelemetry configuration without ServiceDefaults - Rejected due to increased complexity and maintenance burden compared to using Aspire's standardized patterns
- **ALT-002**: Use Application Insights SDK directly - Rejected as OpenTelemetry provides a vendor-neutral approach with support for multiple backends
- **ALT-003**: Skip AppHost and only use ServiceDefaults - Considered but rejected as AppHost provides the dashboard visualization which is valuable for development and debugging
- **ALT-004**: Use ApplicationInsights.WorkerService package - Rejected as Azure.Monitor.OpenTelemetry.AspNetCore is Microsoft's recommended approach for .NET 9
- **ALT-005**: Custom health check endpoints - Rejected in favor of standardized health check middleware from Aspire

## 4. Dependencies

- **DEP-001**: .NET 9.0 SDK - Required for building and running .NET Aspire applications
- **DEP-002**: Docker Desktop or Podman - Required for running the .NET Aspire dashboard
- **DEP-003**: Microsoft.Extensions.Hosting 9.0.0+ - Foundation for hosting configuration
- **DEP-004**: OpenTelemetry.Extensions.Hosting 1.12.0+ - Core OpenTelemetry integration
- **DEP-005**: OpenTelemetry.Instrumentation.Http 1.12.0+ - For HTTP client instrumentation
- **DEP-006**: OpenTelemetry.Instrumentation.Runtime 1.12.0+ - For runtime metrics
- **DEP-007**: OpenTelemetry.Exporter.OpenTelemetryProtocol 1.12.0+ - For OTLP export
- **DEP-008**: Azure.Monitor.OpenTelemetry.AspNetCore 1.3.0+ - For Azure Monitor integration
- **DEP-009**: Microsoft.Extensions.Http.Resilience 9.0.0+ - For resilient HTTP operations
- **DEP-010**: Microsoft.Extensions.ServiceDiscovery 9.0.0+ - For service discovery
- **DEP-011**: Microsoft.AspNetCore.App - Framework reference for ServiceDefaults
- **DEP-012**: Aspire.Hosting - Required for AppHost project

## 5. Files

- **FILE-001**: src/GenAIDBExplorer/GenAIDBExplorer.ServiceDefaults/GenAIDBExplorer.ServiceDefaults.csproj - New ServiceDefaults project file
- **FILE-002**: src/GenAIDBExplorer/GenAIDBExplorer.ServiceDefaults/Extensions.cs - ServiceDefaults extension methods
- **FILE-003**: src/GenAIDBExplorer/GenAIDBExplorer.ServiceDefaults/TelemetryConstants.cs - Constants for telemetry sources and meters
- **FILE-004**: src/GenAIDBExplorer/GenAIDBExplorer.AppHost/GenAIDBExplorer.AppHost.csproj - New AppHost project file
- **FILE-005**: src/GenAIDBExplorer/GenAIDBExplorer.AppHost/Program.cs - AppHost configuration
- **FILE-006**: src/GenAIDBExplorer/GenAIDBExplorer.AppHost/Properties/launchSettings.json - AppHost launch configuration
- **FILE-007**: src/GenAIDBExplorer/GenAIDBExplorer.Console/GenAIDBExplorer.Console.csproj - Updated with ServiceDefaults reference
- **FILE-008**: src/GenAIDBExplorer/GenAIDBExplorer.Console/Program.cs - Updated to use AddServiceDefaults
- **FILE-009**: src/GenAIDBExplorer/GenAIDBExplorer.Core/Telemetry/TelemetryService.cs - New service for custom instrumentation
- **FILE-010**: src/GenAIDBExplorer/GenAIDBExplorer.Core/Telemetry/ActivityExtensions.cs - Helper extensions for Activity objects
- **FILE-011**: src/GenAIDBExplorer/GenAIDBExplorer.Core/Health/HealthCheckExtensions.cs - Health check extensions
- **FILE-012**: src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.ServiceDefaults.Test/ServiceDefaultsTests.cs - Tests for ServiceDefaults
- **FILE-013**: src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/Telemetry/TelemetryServiceTests.cs - Tests for telemetry services
- **FILE-014**: src/GenAIDBExplorer/GenAIDBExplorer.sln - Updated solution file with new projects
- **FILE-015**: docs/MONITORING.md - New documentation for telemetry and monitoring

## 6. Testing

- **TEST-001**: ServiceDefaultsTests - Verify OpenTelemetry configuration
- **TEST-002**: TelemetryServiceTests - Verify custom instrumentation
- **TEST-003**: ActivitySourceTests - Verify activity sources are registered and usable
- **TEST-004**: MeterTests - Verify meters are registered and usable
- **TEST-005**: HealthCheckTests - Verify health checks report correct status
- **TEST-006**: ConsoleAppIntegrationTests - Verify console app works with ServiceDefaults
- **TEST-007**: AppHostIntegrationTests - Verify AppHost orchestration
- **TEST-008**: OpenTelemetryExporterTests - Verify exporters configuration
- **TEST-009**: TelemetryPerformanceTests - Measure telemetry overhead
- **TEST-010**: AzureMonitorIntegrationTests - Verify Azure Monitor export (requires connection string)

## 7. Risks & Assumptions

- **RISK-001**: Framework references to Microsoft.AspNetCore.App may impact console application - Mitigated by careful integration and testing of ServiceDefaults
- **RISK-002**: Performance impact of OpenTelemetry instrumentation - Mitigated by implementing appropriate sampling and testing telemetry overhead
- **RISK-003**: Breaking changes in OpenTelemetry packages - Mitigated by pinning to specific versions and comprehensive testing
- **RISK-004**: Docker dependency for AppHost dashboard - Mitigated by ensuring application works without AppHost
- **ASSUMPTION-001**: Application can be refactored to use HostApplicationBuilder without breaking existing functionality
- **ASSUMPTION-002**: Core operations can be instrumented without major refactoring
- **ASSUMPTION-003**: Existing error handling can be integrated with OpenTelemetry
- **ASSUMPTION-004**: Console application can function both as a hosted service and as a command-line tool

## 8. Related Specifications / Further Reading

- [Adding .NET Aspire Support to GenAIDBExplorer Specification](../spec/spec-infrastructure-dotnet-aspire-integration.md)
- [.NET Aspire overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [Adding .NET Aspire to an existing app](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/add-aspire-existing-app)
- [.NET Aspire service defaults](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults)
- [.NET Aspire telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry)
- [Azure Monitor OpenTelemetry](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable)
- [.NET observability with OpenTelemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel)
