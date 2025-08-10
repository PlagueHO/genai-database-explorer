---
title: SemanticVectors - Technical Documentation
component_path: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors
version: 1.0
date_created: 2025-08-10
last_updated: 2025-08-10
owner: GenAIDBExplorer Core Team
tags: [component, vector, embeddings, search, infrastructure, documentation, architecture, gpt-4.1, developer-flow-gpt-4-1]
---

# SemanticVectors Documentation

*This document was created by GPT-4.1 in developer-flow-gpt-4-1 chat mode.*

SemanticVectors is a modular subsystem for vector embedding, indexing, and search within the GenAIDBExplorer platform. It provides a pluggable, provider-agnostic architecture for generating, storing, and searching vector representations of semantic model entities, supporting both local and cloud-based vector stores.

## 1. Component Overview

### Purpose/Responsibility
- OVR-001: Provides end-to-end vector embedding, indexing, and search for semantic model entities (tables, views, stored procedures).
- OVR-002: Scope includes embedding generation, index writing, search, orchestration, provider selection, and configuration validation. Excludes direct database access and UI concerns.
- OVR-003: Operates as a core infrastructure layer, integrating with the semantic model, project settings, and external vector/embedding providers.

## 2. Architecture Section

- ARC-001: **Design Patterns:**
  - Factory (infrastructure creation)
  - Policy (provider selection/validation)
  - Repository (index writers)
  - Dependency Injection (all services)
  - Options/Validation (configuration)
- ARC-002: **Dependencies:**
  - Internal: SemanticModel, ProjectSettings, SecureJsonSerializer
  - External: Microsoft Semantic Kernel, Microsoft.Extensions.AI, Microsoft.Extensions.VectorData
- ARC-003: **Component Interactions:**
  - Orchestrator triggers vector generation, which uses embedding generator, key builder, record mapper, and index writer. Search services query the index. Infrastructure and policy abstract provider details.
- ARC-004: **Visual Diagrams:** See below.
- ARC-005: **Mermaid Diagram:**

### Component Structure and Dependencies Diagram

```mermaid
graph TD
    subgraph "SemanticVectors System"
        O[VectorOrchestrator] --> G[VectorGenerationService]
        G --> EG[IEmbeddingGenerator]
        G --> KB[IEntityKeyBuilder]
        G --> RM[IVectorRecordMapper]
        G --> IW[IVectorIndexWriter]
        G --> IF[IVectorInfrastructureFactory]
        G --> PS[ProjectSettings]
        G --> SJ[ISecureJsonSerializer]
        O --> S[IVectorSearchService]
    end

    subgraph "External Dependencies"
        SK[Microsoft Semantic Kernel]
        AI[Microsoft.Extensions.AI]
        VD[Microsoft.Extensions.VectorData]
    end

    EG --> SK
    IW --> VD
    S --> VD
    IF --> AI

    classDiagram
        class VectorOrchestrator {
            +GenerateAsync(...)
        }
        class VectorGenerationService {
            +GenerateAsync(...)
        }
        class IEmbeddingGenerator {
            +GenerateAsync(...)
        }
        class IVectorIndexWriter {
            +UpsertAsync(...)
        }
        class IVectorSearchService {
            +SearchAsync(...)
        }
        class IVectorInfrastructureFactory {
            +Create(...)
        }
        class IVectorIndexPolicy {
            +ResolveProvider(...)
            +Validate(...)
        }
        class IVectorRecordMapper {
            +BuildEntityText(...)
            +ToRecord(...)
        }
        class IEntityKeyBuilder {
            +BuildKey(...)
            +BuildContentHash(...)
        }

        VectorOrchestrator --> VectorGenerationService
        VectorGenerationService --> IEmbeddingGenerator
        VectorGenerationService --> IVectorIndexWriter
        VectorGenerationService --> IVectorInfrastructureFactory
        VectorGenerationService --> IVectorRecordMapper
        VectorGenerationService --> IEntityKeyBuilder
        VectorGenerationService --> ISecureJsonSerializer
        VectorOrchestrator --> IVectorSearchService
```

## 3. Interface Documentation

| Method/Property | Purpose | Parameters | Return Type | Usage Notes |
|-----------------|---------|------------|-------------|-------------|
| IEmbeddingGenerator.GenerateAsync | Generate embedding vector for text | string text, VectorInfrastructure, CancellationToken | Task<ReadOnlyMemory<float>> | Async, uses configured embedding service |
| IVectorIndexWriter.UpsertAsync | Upsert vector record into index | EntityVectorRecord, VectorInfrastructure, CancellationToken | Task | Async, provider-agnostic |
| IVectorSearchService.SearchAsync | Search for similar vectors | ReadOnlyMemory<float> vector, int topK, VectorInfrastructure, CancellationToken | Task<IEnumerable<(EntityVectorRecord, double)>> | Returns top-K matches by similarity |
| IVectorOrchestrator.GenerateAsync | Orchestrate vector generation for model | SemanticModel, DirectoryInfo, VectorGenerationOptions, CancellationToken | Task<int> | Returns count of processed entities |
| IVectorGenerationService.GenerateAsync | Generate vectors for all entities | SemanticModel, DirectoryInfo, VectorGenerationOptions, CancellationToken | Task<int> | Core business logic |
| IVectorInfrastructureFactory.Create | Create provider-specific infrastructure | VectorIndexSettings, string repositoryStrategy | VectorInfrastructure | Factory pattern |
| IVectorIndexPolicy.ResolveProvider | Select effective vector provider | VectorIndexSettings, string repositoryStrategy | string | Policy pattern |
| IVectorIndexPolicy.Validate | Validate provider/settings compatibility | VectorIndexSettings, string repositoryStrategy | void | Throws on invalid config |
| IVectorRecordMapper.BuildEntityText | Build canonical text for entity | SemanticModelEntity | string | Used for embedding input |
| IVectorRecordMapper.ToRecord | Map entity to vector record | SemanticModelEntity, string id, string content, ReadOnlyMemory<float> vector, string contentHash | EntityVectorRecord | For index storage |
| IEntityKeyBuilder.BuildKey | Build unique key for entity | modelName, entityType, schema, name | string | Normalized, deterministic |
| IEntityKeyBuilder.BuildContentHash | Hash entity content | string content | string | SHA256, for change detection |

## 4. Implementation Details

- IMP-001: **Main Classes:**
  - `SemanticKernelEmbeddingGenerator`, `InMemoryVectorIndexWriter`, `SkInMemoryVectorIndexWriter`, `InMemoryVectorSearchService`, `SkInMemoryVectorSearchService`, `VectorOrchestrator`, `VectorGenerationService`, `VectorInfrastructureFactory`, `VectorIndexPolicy`, `VectorRecordMapper`, `EntityKeyBuilder`
- IMP-002: **Configuration:**
  - Uses .NET Options pattern (`VectorIndexOptions`), validated at startup by `VectorOptionsValidator`. Provider, collection name, embedding service, and provider-specific settings are configurable.
- IMP-003: **Key Algorithms:**
  - Embedding generation via Semantic Kernel or Microsoft.Extensions.AI
  - Indexing via in-memory or provider-specific writers
  - Search via cosine similarity or provider-native search
  - Deterministic key and content hash generation for idempotency
- IMP-004: **Performance:**
  - Async/await throughout for scalability
  - In-memory implementations for dev/test; pluggable for production
  - Efficient change detection via content hash

## 5. Usage Examples

### Basic Usage

```csharp
// Generate and index vectors for a semantic model
var orchestrator = new VectorOrchestrator(generationService, logger);
int processed = await orchestrator.GenerateAsync(model, projectPath, options, cancellationToken);
```

### Advanced Usage

```csharp
// Custom provider and options
var options = new VectorGenerationOptions { Overwrite = true, SkipTables = false };
var infraFactory = new VectorInfrastructureFactory(policy);
var infra = infraFactory.Create(settings, "AzureBlob");
var embedding = await embeddingGenerator.GenerateAsync("Sample text", infra, cancellationToken);
```

- USE-001: Use dependency injection for all services
- USE-002: Validate configuration at startup
- USE-003: Prefer async methods for all operations

## 6. Quality Attributes

- QUA-001: **Security:**
  - Input validation, deterministic keying, secure serialization
- QUA-002: **Performance:**
  - Async/await, efficient in-memory and provider-based implementations
- QUA-003: **Reliability:**
  - Exception handling, configuration validation, idempotent operations
- QUA-004: **Maintainability:**
  - Modular interfaces, clear separation of concerns, testable via DI
- QUA-005: **Extensibility:**
  - Pluggable providers, policy-driven selection, open for new index/search backends

## 7. Reference Information

- REF-001: **Dependencies:**
  - Microsoft.SemanticKernel
  - Microsoft.Extensions.AI
  - Microsoft.Extensions.VectorData
- REF-002: **Configuration:**
  - See `VectorIndexOptions` for all available settings
- REF-003: **Testing:**
  - Use Moq for interfaces, test in-memory implementations for fast feedback
- REF-004: **Troubleshooting:**
  - Check logs for provider selection and validation errors
  - Ensure expected vector dimensions match embedding model
- REF-005: **Related Docs:**
  - [spec-data-vector-embeddings-and-indexing.md](../../spec/spec-data-vector-embeddings-and-indexing.md)
  - [project-model-documentation.md](./project-model-documentation.md)
- REF-006: **Change History:**
  - Initial version (2025-08-10, GPT-4.1, developer-flow-gpt-4-1)
