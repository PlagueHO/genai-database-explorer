---
title: SemanticVectors - Technical Documentation
component_path: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors
version: 1.0
date_created: 2025-08-10
last_updated: 2025-08-10
owner: PlagueHO Team
tags: [component, service, vector, embedding, search, infrastructure, documentation, architecture, gpt-4.1, developer-flow-gpt-4-1]
---



*This document was created by GPT-4.1 in developer-flow-gpt-4-1 chat mode.*

SemanticVectors is a modular subsystem for generating, storing, and searching vector embeddings for database entities. It enables semantic search and similarity operations over structured data, supporting multiple vector index providers and embedding services. The component is designed for extensibility, maintainability, and integration with the broader GenAIDBExplorer architecture.

## 1. Component Overview


### Purpose/Responsibility

- OVR-001: Provides vector embedding generation and indexing for semantic model entities (tables, views, stored procedures).
- OVR-002: Scope includes orchestration, embedding generation, mapping, indexing, and provider abstraction. Excludes direct user interface and database schema extraction.
- OVR-003: Operates as a backend service, integrating with the semantic model and project configuration. Interacts with external vector stores and embedding services.

## 2. Architecture Section

- ARC-001: Design patterns used: Factory (infrastructure, embedding generator), Strategy (indexing, provider selection), Policy (provider validation), Dependency Injection, Interface Segregation.
- ARC-002: Internal dependencies: Orchestrators, Generation Services, Mappers, Index Writers, Embedding Generators, Infrastructure Factories, Policy Validators. External dependencies: Microsoft.SemanticKernel, Microsoft.Extensions.AI, Microsoft.Extensions.VectorData, file system, Azure AI Search, Azure CosmosDB.
- ARC-003: Components interact via interfaces, enabling testability and provider swapping. Orchestrator coordinates generation, which flows through mappers, embedding generators, and index writers.
- ARC-004: See mermaid diagram below for structure and relationships.
- ARC-005: Mermaid diagram illustrates main classes, interfaces, dependencies, and data flow.

### Component Structure and Dependencies Diagram

```mermaid
graph TD
    subgraph "SemanticVectors System"
        A[VectorOrchestrator] --> B[VectorGenerationService]
        B --> C[IVectorRecordMapper]
        B --> D[IEmbeddingGenerator]
        B --> E[IVectorIndexWriter]
        B --> F[IVectorInfrastructureFactory]
        F --> G[IVectorIndexPolicy]
    end

    subgraph "External Dependencies"
        H[Microsoft.SemanticKernel]
        I[Microsoft.Extensions.AI]
        J[Microsoft.Extensions.VectorData]
        K[Azure AI Search]
        L[Azure CosmosDB]
    end

    D --> H
    D --> I
    E --> J
    F --> K
    F --> L

    classDiagram
        class VectorOrchestrator {
            +GenerateAsync(...)
        }
        class VectorGenerationService {
            +GenerateAsync(...)
        }
        class IVectorRecordMapper {
            +BuildEntityText(...)
            +ToRecord(...)
        }
        class IEmbeddingGenerator {
            +GenerateAsync(...)
        }
        class IVectorIndexWriter {
            +UpsertAsync(...)
        }
        class IVectorInfrastructureFactory {
            +Create(...)
        }
        class IVectorIndexPolicy {
            +ResolveProvider(...)
            +Validate(...)
        }
        VectorOrchestrator --> VectorGenerationService
        VectorGenerationService --> IVectorRecordMapper
        VectorGenerationService --> IEmbeddingGenerator
        VectorGenerationService --> IVectorIndexWriter
        VectorGenerationService --> IVectorInfrastructureFactory
        IVectorInfrastructureFactory --> IVectorIndexPolicy
```

## 3. Interface Documentation

- INT-001: Public interfaces:
    - `IVectorOrchestrator`: Orchestrates vector generation.
    - `IVectorGenerationService`: Implements generation logic.
    - `IEmbeddingGenerator`: Generates vector embeddings from text.
    - `IVectorRecordMapper`: Maps entities to vector records.
    - `IVectorIndexWriter`: Persists vectors to index.
    - `IVectorInfrastructureFactory`: Creates provider-specific infrastructure.
    - `IVectorIndexPolicy`: Validates and resolves provider selection.
- INT-002: Method/property reference table:

| Method/Property | Purpose | Parameters | Return Type | Usage Notes |
|-----------------|---------|------------|-------------|-------------|
| GenerateAsync (VectorOrchestrator) | Orchestrate vector generation | SemanticModel, DirectoryInfo, VectorGenerationOptions, CancellationToken | Task&lt;int&gt; | Entry point for vector generation |
| GenerateAsync (VectorGenerationService) | Generate and persist vectors | SemanticModel, DirectoryInfo, VectorGenerationOptions, CancellationToken | Task&lt;int&gt; | Main implementation |
| GenerateAsync (IEmbeddingGenerator) | Generate embedding vector | string text, VectorInfrastructure, CancellationToken | Task&lt;ReadOnlyMemory&lt;float&gt;&gt; | Uses Semantic Kernel or provider |
| UpsertAsync (IVectorIndexWriter) | Persist vector record | EntityVectorRecord, VectorInfrastructure, CancellationToken | Task | Supports multiple index providers |
| BuildEntityText (IVectorRecordMapper) | Build text for embedding | SemanticModelEntity | string | Used for embedding input |
| ToRecord (IVectorRecordMapper) | Map entity to vector record | SemanticModelEntity, id, content, vector, contentHash | EntityVectorRecord | For index persistence |
| Create (IVectorInfrastructureFactory) | Create vector infra | VectorIndexSettings, repositoryStrategy | VectorInfrastructure | Provider abstraction |
| ResolveProvider (IVectorIndexPolicy) | Select provider | VectorIndexSettings, repositoryStrategy | string | Policy-based selection |
| Validate (IVectorIndexPolicy) | Validate provider/settings | VectorIndexSettings, repositoryStrategy | void | Throws on invalid config |

- INT-003: No events/callbacks; all operations are async and return Tasks.

## 4. Implementation Details

- IMP-001: Main classes: `VectorOrchestrator`, `VectorGenerationService`, `SemanticKernelEmbeddingGenerator`, `VectorRecordMapper`, `InMemoryVectorIndexWriter`, `VectorInfrastructureFactory`, `VectorIndexPolicy`.
- IMP-002: Configuration via `ProjectSettings` and `VectorIndexOptions` (provider, collection, embedding service, etc.). Initialization via dependency injection.
- IMP-003: Key logic: orchestrator triggers generation, which selects entities, builds text, generates embeddings, persists envelopes, and upserts to index. Handles dry-run, overwrite, and content hash checks.
- IMP-004: Performance: supports async/await, parallel entity processing, and provider abstraction for scalability. Bottlenecks may occur in embedding generation or external index writes.

## 5. Usage Examples

### Basic Usage

```csharp
// Basic usage example
var orchestrator = new VectorOrchestrator(generationService, logger);
await orchestrator.GenerateAsync(model, projectPath, options);
```

### Advanced Usage

```csharp
// Advanced configuration patterns
var options = new VectorGenerationOptions { Overwrite = true, DryRun = false };
var orchestrator = new VectorOrchestrator(generationService, logger);
await orchestrator.GenerateAsync(model, projectPath, options);
```

- USE-001: Use dependency injection for all services.
- USE-002: Configure providers and embedding services via appsettings or ProjectSettings.
- USE-003: Best practice: validate options at startup, use async methods, handle exceptions and logging.

## 6. Quality Attributes

- QUA-001: Security: No direct user input; relies on configuration. Embedding and index providers should be secured (API keys, connection strings).
- QUA-002: Performance: Async/await throughout, supports parallelism, provider abstraction for scaling.
- QUA-003: Reliability: Exception handling, logging, dry-run mode, content hash checks to avoid redundant work.
- QUA-004: Maintainability: SOLID, DRY, clear interfaces, modular structure, testable via dependency injection and mocks.
- QUA-005: Extensibility: Add new providers, embedding generators, or mappers by implementing interfaces.

## 7. Reference Information

- REF-001: Dependencies:
    - Microsoft.SemanticKernel (embedding)
    - Microsoft.Extensions.AI (embedding abstraction)
    - Microsoft.Extensions.VectorData (vector index abstraction)
    - Azure AI Search, Azure CosmosDB (optional providers)
- REF-002: Configuration options: see `VectorIndexOptions` (provider, collection, embeddingServiceId, expectedDimensions, hybrid, etc.)
- REF-003: Testing: Use mocks for interfaces, test dry-run and overwrite logic, validate provider selection and error handling.
- REF-004: Troubleshooting: Check logs for provider errors, ensure configuration is valid, validate embedding service connectivity.
- REF-005: Related docs: See project README, semantic-model-documentation.md, and technical/SEMANTIC_MODEL_PROJECT_STRUCTURE.md
- REF-006: Change history: See repository commit log for updates to SemanticVectors subsystem.
