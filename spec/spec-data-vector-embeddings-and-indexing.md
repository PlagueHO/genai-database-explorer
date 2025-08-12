---
title: Data Vector Embeddings and Indexing Specification
version: 1.0
date_created: 2025-08-08
last_updated: 2025-08-12
owner: GenAI Database Explorer Team
tags: [data, design, vector, embeddings, search, semantic-kernel, azure, ai-search, cosmosdb]
---

## Introduction

This specification defines the architecture, patterns, interfaces, and behaviors required to add vector embeddings and vector search over semantic model entities (tables, views, stored procedures). It aligns with existing repository strategies (Local Disk, Azure Blob Storage, CosmosDB) and uses Semantic Kernel vector-store connectors with Microsoft.Extensions.VectorData abstractions.

The goals are to:

- Generate embeddings from entity descriptions and related context.
- Persist embeddings alongside entities where appropriate.
- Expose consistent vector search across connectors (CosmosDB, Azure AI Search, and an in-memory store for tests).
- Provide a CLI command to generate and optionally push vectors to an index.

## 1. Purpose & Scope

Purpose: Enable efficient natural language discovery of semantic model entities via vector embeddings and similarity search.

Scope:

- Embedding generation pipeline and configuration.
- Storage strategy–aware persistence of embeddings.
- Vector index provider selection and search.
- CLI orchestration for generating/pushing vectors.

Audience: Developers, architects, AI engineers working on GenAI Database Explorer.

Assumptions: .NET 9, C# 11+, DI, async/await, existing project settings and DI patterns, Semantic Kernel usage per repository conventions.

## 2. Definitions

- Embedding: Numeric vector representation of text used for similarity search.
- Vector Index: Data structure supporting efficient vector similarity queries.
- SK: Microsoft Semantic Kernel (connectors and AI services).
- Vector Store Connector: SK connector implementing VectorData abstractions.
- Local/Blob Strategy: Semantic model persisted as JSON files on disk or in Azure Blob.
- CosmosDB Strategy: Semantic model persisted in Azure CosmosDB.
- InMemory Store: Semantic Kernel InMemory vector store connector used for testing/dev (Volatile is legacy/obsolete).
- Entity: SemanticModelEntity (Table, View, StoredProcedure) within the semantic model.

## 3. Requirements, Constraints & Guidelines

### Core Requirements

- REQ-001: Generate embeddings for entities based on enriched descriptions and structural context.
- REQ-002: Use Microsoft.Extensions.VectorData.Abstractions for record/collection modeling.
- REQ-003: Use Semantic Kernel vector-store connectors for CosmosDB, Azure AI Search, and the SK InMemory connector for tests/dev. Do not implement a custom in-memory indexer. Do not use Microsoft.Extensions.AI for vector storage.
- REQ-004: Provide a new CLI command generate-vectors to compute/update embeddings post enrich-model and data-dictionary.
- REQ-005: For Local/Blob strategies, persist embedding floats and metadata with the entity JSON; optionally push to external index (AI Search or In-Memory).
- REQ-006: For CosmosDB strategy, store vectors in the SAME CosmosDB container and documents as the semantic model entities (same-container colocation). Persist the embedding floats in a configured vector field path on the entity document and maintain embedding metadata alongside. Do not use a separate vector container.
- REQ-007: Provide a search API that returns top-k relevant entities by vector similarity, independent of the underlying index provider.
- REQ-008: Ensure idempotent embedding updates via content hashing and embedding metadata (model, dims, timestamp, version).
- REQ-009: Dimension, model/deployment, and index configuration must validate at startup and fail-fast on mismatch.
- REQ-010: Support optional hybrid search (vector + keyword) where the connector supports it (Cosmos/AI Search).

### Security Requirements

- SEC-001: Do not persist secrets in semantic model files; use environment variables/managed identity for service credentials.
- SEC-002: Validate and sanitize IDs/keys used for vector records to prevent injection or index pollution.
- SEC-003: For Local/Blob JSON, ensure safe serialization settings to avoid JSON injection vectors.

### Performance Requirements

- PER-001: Batch embedding requests where possible; skip unchanged entities via content hashing.
- PER-002: Vector search should return results within 500ms p95 for k ≤ 20 under normal conditions in cloud indices.
- PER-003: For Local/Blob, persistence of floats must remain human-readable JSON; consider quantization as a future optimization (not required now).

### Constraints

- CON-001: Align with existing DI and SemanticKernelFactory usage; embeddings must be registered with a serviceId (e.g., "Embeddings").
- CON-002: CosmosDB strategy must use CosmosDB vector indexing on the Entities container (same container as entity documents) with a container-level vector policy; do not support external vector indices concurrently, and do not use a separate CosmosDB container for vectors.
- CON-003: Local/Blob may use external index (AI Search) or in-memory; floats must still be persisted locally.
- CON-004: Embedding dimension must match the configured embedding model.
- CON-005: For in-memory vectors, use the SK InMemory connector via DI (services.AddInMemoryVectorStore()) or direct types (InMemoryVectorStore/InMemoryCollection). The Volatile connector is obsolete and must not be used.

### Guidelines

- GUD-001: Follow Clean Architecture boundaries—use adapters/mappers for translating domain entities to vector records.
- GUD-002: Keep domain model decoupled from index providers; put provider logic behind a Strategy interface.
- GUD-003: Use structured logging with scopes and include token usage and index operation metrics when available.
- GUD-004: Prefer immutable DTOs for vector records and metadata.

### Patterns

- PAT-001: Strategy for selecting the vector index provider based on repository strategy and settings.
- PAT-002: Abstract Factory to build embedding generator and vector collection instances from config/DI.
- PAT-003: Adapter/Mapper from domain entities to vector records (VectorData attributes) and text payloads.
- PAT-004: Facade for orchestration (generate and search) to hide connector details from callers.
- PAT-005: Template Method/Pipeline for the generate-vectors flow (select → build text → embed → persist → upsert).

## 4. Interfaces & Data Contracts

### Interfaces

```csharp
// Decides allowed index provider given repository strategy and config
public interface IVectorIndexPolicy
{
  VectorIndexProvider ResolveProvider(); // Auto|CosmosDB|AzureAISearch|InMemory
    bool AllowExternalIndexForRepo(string repoStrategy);
}

// Creates embedding generator + vector store collection using DI/config
public interface IVectorInfrastructureFactory
{
    IEmbeddingGenerator CreateEmbeddingGenerator();
    Microsoft.Extensions.VectorData.VectorStoreCollection<string, EntityVectorRecord> CreateCollection();
}

// Converts domain entities into records + text builder for embeddings
public interface IVectorRecordMapper
{
    string BuildEntityText(GenAIDBExplorer.Core.Models.SemanticModel.SemanticModelEntity entity);
    EntityVectorRecord ToRecord(
        GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel model,
        GenAIDBExplorer.Core.Models.SemanticModel.SemanticModelEntity entity,
        ReadOnlyMemory<float> embedding);
}

// High-level orchestration: generate & search
// Thin orchestrator: sequences services, owns no business logic
public interface IVectorOrchestrator
{
  Task<int> GenerateAsync(
    GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel model,
    IEnumerable<GenAIDBExplorer.Core.Models.SemanticModel.SemanticModelEntity> entities,
    bool push,
    bool overwrite,
    CancellationToken ct);

  Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
    string modelName,
    string queryText,
    int k,
    CancellationToken ct);
}

// Split services for SRP/testability
public interface IVectorGenerationService
{
  Task<int> GenerateEmbeddingsAsync(
    GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel model,
    IEnumerable<GenAIDBExplorer.Core.Models.SemanticModel.SemanticModelEntity> entities,
    bool overwrite,
    CancellationToken ct);
}

public interface IVectorIndexWriter
{
  Task<int> UpsertAsync(IEnumerable<object> records, CancellationToken ct);
}

public interface IVectorSearchService
{
  Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
    string modelName,
    string queryText,
    int k,
    CancellationToken ct);
}

public interface IEmbeddingGenerator
{
    Task<ReadOnlyMemory<float>> GenerateAsync(string text, CancellationToken ct);
}
```

### Data Contracts

```csharp
using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;

// Domain-level search result (independent of storage DTO)
public sealed record VectorSearchHit(
  string Id,
  string Model,
  string EntityType,
  string Schema,
  string Name,
  double Score);

// Storage-mapped record (adapter). Vector dimension is defined by index schema
// and validated at startup; do not hard-code dimensions in attributes.
public sealed class EntityVectorRecord
{
    [VectorStoreKey]
    public required string Id { get; init; } // {model}:{type}:{schema}.{name}

    [VectorStoreData(IsIndexed = true)]
    public required string Model { get; init; }

    [VectorStoreData(IsIndexed = true)]
    public required string EntityType { get; init; } // Table|View|StoredProcedure

    [VectorStoreData(IsIndexed = true)]
    public required string Schema { get; init; }

    [VectorStoreData(IsFullTextIndexed = true)]
    public required string Name { get; init; }

  [VectorStoreData(IsFullTextIndexed = true)]
  public required string Text { get; init; }

  // Vector field mapped by provider index schema; dimension validated at startup
  public required ReadOnlyMemory<float> Embedding { get; init; }

    [VectorStoreData]
    public string? EmbeddingModel { get; init; }

    [VectorStoreData]
    public DateTimeOffset LastUpdatedUtc { get; init; }
}

public sealed record EmbeddingMetadata(
  string Model,
  int Dimensions,
  string ContentHash,
  DateTimeOffset LastUpdatedUtc,
  string? Version = null
);
```

### Provider Enum

```csharp
public enum VectorIndexProvider
{
  Auto,
  CosmosDB,
  AzureAISearch,
  InMemory
}
```

### Configuration Shape (Project Settings)

```json
{
  "VectorIndex": {
  "Provider": "Auto|CosmosDB|AzureAISearch|InMemory",
    "CollectionName": "entities",
    "PushOnGenerate": true,
  "ProvisionIfMissing": false,
  "AllowedForRepository": "Auto|CosmosDBOnly|ExternalAllowed",
    "AzureAISearch": {
      "Endpoint": "https://<name>.search.windows.net",
      "ApiKey": "env:AZURE_SEARCH_KEY",
      "IndexName": "sk-entities"
    },
  "CosmosDB": {
      // When using Cosmos strategy, vectors are stored in the same container as entity documents
      // (SemanticModelRepository.CosmosDb.EntitiesContainerName). Configure only vector specifics here.
      "VectorPath": "/embedding/vector",          // JSON path on the entity document holding the vector
      "DistanceFunction": "cosine",               // cosine | dotproduct | euclidean
      "IndexType": "diskANN"                      // diskANN | quantizedFlat | flat
    },
  "EmbeddingServiceId": "Embeddings",
  "ExpectedDimensions": 1536
  }
}
```

## 5. Acceptance Criteria

- AC-001: Given Local/Blob repository, When generate-vectors runs without --push, Then embeddings are computed for changed entities and persisted as floats + metadata in entity JSON.
- AC-002: Given Local/Blob repository and PushOnGenerate=true or --push, When generate-vectors runs, Then records are upserted into the configured external index (AI Search or InMemory) and local JSON is updated.
- AC-003: Given CosmosDB repository, When generate-vectors runs, Then the embedding floats are written to the configured vector field on the SAME entity documents in the Entities container (with container-level vector policy) and metadata is updated; no separate vector container is used.
- AC-004: Given unchanged entity text and no --overwrite, When generate-vectors runs, Then embeddings are skipped (idempotent via content hash).
- AC-005: Given a natural language query, When SearchAsync is called, Then top-k relevant entities are returned consistently across providers.
- AC-006: Given invalid dimension/model config, When app starts, Then it fails-fast with a clear error.
- AC-007: Given VectorIndex.ProvisionIfMissing=true and an index/container does not exist, When generate-vectors or search runs, Then the runtime attempts to create the minimal required index/container and proceeds; otherwise it fails-fast with a clear error.

## 6. Test Automation Strategy

- Test Levels: Unit (mappers, policy, orchestrator), Integration (AI Search, Cosmos), CLI integration (Pester) for generate-vectors.
- Frameworks: MSTest, FluentAssertions, Moq; Pester for CLI flows.
- Test Data Management: Use sample semantic models under samples/; seed in-memory vector store for deterministic tests.
- CI/CD Integration: Gate optional Azure integration tests by env vars; always run in-memory unit tests.
- Coverage Requirements: ≥80% on new vector orchestration and mapping code.
- Performance Testing: Micro-benchmarks for mapper and orchestrator; optional load for search (AI Search/Cosmos) outside CI.

## 7. Rationale & Context

- Using VectorData abstractions and SK connectors ensures provider-agnostic modeling and straightforward DI integration.
- Persisting floats in Local/Blob supports offline analysis and reproducibility; deferring to Cosmos vectors eliminates duplication where native indexing exists.
- Strategy + Facade patterns keep complexity hidden from CLI and callers, supporting maintainability and testability.

## 8. Dependencies & External Integrations

### External Systems

- EXT-001: Azure AI Search — vector/hybrid indexing and similarity search.
- EXT-002: Azure CosmosDB for NoSQL — native vector indexing and search.

### Third-Party Services

- SVC-001: Semantic Kernel Vector Store Connectors — Azure AI Search, CosmosDB NoSQL, InMemory.

### Infrastructure Dependencies

- INF-001: Azure credentials via env/managed identity; network egress to Azure services when enabled.

### Data Dependencies

- DAT-001: Semantic model entities and enriched descriptions as embedding source text.

### Technology Platform Dependencies

- PLT-001: .NET 9; Microsoft.SemanticKernel; Microsoft.Extensions.VectorData.Abstractions.
- PLT-002: Microsoft.SemanticKernel.Connectors.InMemory for in-memory vector store support (prefer DI: services.AddInMemoryVectorStore()).

### Compliance Dependencies

- COM-001: Respect data privacy policies; avoid embedding PII unless explicitly approved.

## 9. Examples & Edge Cases

```csharp
// Orchestrated generation sketch
var entities = semanticModel.Tables.Cast<SemanticModelEntity>()
    .Concat(semanticModel.Views)
    .Concat(semanticModel.StoredProcedures);

var updated = await vectorOrchestrator.GenerateAsync(
    semanticModel,
    entities,
    push: settings.VectorIndex.PushOnGenerate,
    overwrite: false,
    ct: CancellationToken.None);

// Search
var results = await vectorOrchestrator.SearchAsync(
    semanticModel.Name,
    "average entitlement value for US customers with HQ in Europe",
    k: 10,
    ct: CancellationToken.None);
```

Edge cases:

- Entity renamed: record Id must be deterministic and update-safe; upsert handles replacement.
- Embedding model changed: content hash and metadata force regeneration.
- Large descriptions: truncate/segment source text before embedding (implementation detail; not mandated here).

## 10. Validation Criteria

- VC-001: Unit tests cover policy resolution, mapping, and idempotent updates.
- VC-002: InMemory tests validate end-to-end generate + search without Azure dependencies.
- VC-003: Optional integration tests validate AI Search and Cosmos paths when credentials are present.
- VC-004: CLI Pester tests invoke generate-vectors and assert updated artifacts/logs.

## 11. Related Specifications / Further Reading

- [spec-data-semantic-model-repository.md](spec/spec-data-semantic-model-repository.md)
- [spec-data-natural-language-query-provider.md](spec/spec-data-natural-language-query-provider.md)
- [Microsoft Learn: Semantic Kernel Vector Store Connectors (Overview)](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors)
- [Azure AI Search Vector Store connector (C#)](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/azure-ai-search-connector)
- [Azure Cosmos DB NoSQL Vector Store connector (C#)](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/azure-cosmosdb-nosql-connector)
- [In-Memory Vector Store connector (C#)](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/inmemory-connector)
- [Volatile Vector Store connector (Preview/obsolete, links to InMemory)](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/volatile-connector)
- [NuGet: Microsoft.Extensions.VectorData.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.VectorData.Abstractions/)

## 12. Additional Implementation Guidance

### 12.1 Entity persistence shape (Local/Blob)

Persist vectors and metadata inside the entity JSON; keep backward compatibility (absent fields are valid). Example:

```json
{
  "schema": "dbo",
  "name": "Customer",
  "semanticDescription": "...",
  "embedding": {
    "vector": [0.01, 0.02, 0.03],
    "metadata": {
      "model": "text-embedding-3-small",
      "dimensions": 1536,
      "contentHash": "sha256:...",
      "lastUpdatedUtc": "2025-08-08T12:00:00Z",
      "version": "v1"
    }
  }
}
```

Notes: human-readable JSON; floats array may be large. If absent, treat as “not embedded yet”.

### 12.2 Provider selection policy (Auto) and precedence

| Repo Strategy | Allowed Providers | Auto Default | Persist Floats in Entity |
|---|---|---|---|
| Local Disk | InMemory, AzureAISearch | AzureAISearch if explicitly configured; otherwise InMemory | Yes |
| Azure Blob | InMemory, AzureAISearch | AzureAISearch if explicitly configured; otherwise InMemory | Yes |
| CosmosDB | CosmosDB | CosmosDB (external disallowed) | Yes (same document field) |

Validation: Cosmos + external provider → fail-fast. For Local/Blob with Provider=Auto: choose the first allowed provider with valid configuration (prefers AzureAISearch when configured; otherwise InMemory). If Provider is explicitly set but misconfigured, fail-fast.

### 12.3 Embedding model and dimension binding

- Embedding model/deployment is configured under OpenAIService.Embedding in settings.json. VectorIndex.EmbeddingServiceId references the registered SK embeddings service (e.g., "Embeddings").
- ExpectedDimensions (optional) declares the vector size the index expects. It must match the embedding model used by the configured deployment.
- On model/dimension change, force regeneration (update EmbeddingMetadata and content hash).

Deterministic dimension logic:

- If VectorIndex.ExpectedDimensions is set, validate equality against the generated embedding length at runtime; mismatch → fail-fast with a clear error.
- If VectorIndex.ExpectedDimensions is omitted, infer from the embedding output length at first generation and persist in metadata; validate on subsequent runs.
- Optionally, a centralized model→dimension map may be provided under OpenAIService to avoid runtime inference in locked-down environments.

### 12.4 Index defaults & ownership

- Azure AI Search: HNSW by default; cosine distance recommended. Analyzer config not supported by connector—provision index via infra (Bicep) when custom analyzers needed.
- CosmosDB: apply a container-level vector policy and vector index on the Entities container (that stores semantic model entity documents). Recommend IndexType=diskANN for large sets, or quantizedFlat/flat per workload, with cosine distance. Define partition key (see 12.11). Prefer provisioning via infra.

### 12.5 CLI: generate-vectors (options & examples)

Options:

- --project PATH (required), --entityType [table|view|storedProcedure|all]=all, --schemaName NAME, --name NAME
- --push (upsert to external index), --overwrite, --dry-run
- --batchSize NUM=32, --maxConcurrency NUM=Environment.ProcessorCount

Exit codes: 0 success; 1 failures encountered. Output summary JSON (generated, skipped, pushed, failed, durationMs).

Example:

```powershell
dotnet run --project GenAIDBExplorer.Console/ -- generate-vectors --project d:/temp --entityType table --push
```

### 12.6 Error handling & retries

- Embeddings and index upserts: retry 3 times with exponential backoff.
- Partial failures: continue and include in summary; --fail-fast stops on first error.

### 12.7 Telemetry & metrics (OpenTelemetry)

- Spans: embeddings.generate, vectors.upsert, vectors.search.
- Attributes: model, dimension, entityType, schema, name, k, durationMs, resultCount, tokenUsage.
- Counters: embeddings.generated, vectors.upserted, vectors.search.requests, vectors.search.duration.

### 12.8 Concurrency & locking

- Per-entity lock to avoid duplicate work in a single run.
- Concurrent CLI runs on same project not recommended; last writer wins. Cosmos upserts are idempotent.

### 12.9 Multiple vectors per record

- v1 supports a single primary embedding per entity. Reserve future fields for additional vectors (e.g., NameEmbedding).

### 12.10 Hybrid search semantics

- Controlled by settings (enableHybrid, keywordWeight). If provider lacks support, ignore gracefully.

### 12.11 CosmosDB partitioning & keys

- Recommended partition key: Model or Model/EntityType to balance queries.
- When a custom partition key is used, prefer CosmosDBCompositeKey for Get/Upsert.
- Example id: "adventureworks:Table:dbo.Customer"; partition: "adventureworks/table".
- Vector field path: configure a JSON path on the entity document (e.g., "/embedding/vector") that matches the container vector policy and indexing policy.

### 12.12 Resource lifecycle & ownership

- Provision Azure AI Search indexes and CosmosDB containers via infra (e.g., infra/main.bicep). Runtime will not configure analyzers or advanced index features. If VectorIndex.ProvisionIfMissing=true, runtime may create minimal resources when absent:
  - CosmosDB: ensure the Entities container exists and has a vector policy for the configured VectorPath and a matching vector index (IndexType, DistanceFunction). No separate vector container is created.

### 12.13 Versioning & migration

- EmbeddingMetadata.Version tracks pipeline changes. Bump on significant changes (text pipeline/model/dimension) to force regeneration.
- Existing models without embeddings: first generate-vectors populates metadata and vectors.

### 12.14 Limits & truncation

- Default source text limit: 8,000 chars. Truncate with ellipsis; optional future chunking not required in v1.

### 12.15 Testing details

- Provide a deterministic IEmbeddingGenerator stub for unit tests.
- Use InMemory vector store for e2e tests; seed known vectors to assert k-NN outcomes.

### 12.16 Naming & sanitization

- Record Id format: "{model}:{type}:{schema}.{name}".
- Sanitize to allowed chars [a-z0-9_:\-.]; lower-case; max length ≤ 128.

### 12.17 Config validation & failure mode

- Validate at startup (options validation/DI init). Fail-fast on Cosmos+external, dimension mismatch, or missing required credentials when provider is explicit.

### 12.18 InMemory vs Volatile

- Prefer the SK InMemory connector for tests/dev. Volatile is legacy/obsolete; referenced only for historical docs. Wire via services.AddInMemoryVectorStore() in DI, or use InMemoryVectorStore/InMemoryCollection directly when DI is not available.

### 12.19 Reconcile-index CLI (Issue 5)

- Purpose: detect and fix drift between local entity metadata/floats and external index.
- Command (spec): reconcile-index --project PATH [--dry-run] [--entityType ...] [--schemaName ...] [--name ...]
- Behavior: compare EmbeddingMetadata.ContentHash and LastUpdatedUtc with index records; upsert missing/outdated records; output a summary JSON (matched, updated, missing, errors). Dry-run reports differences without changes.

### 12.20 Telemetry naming standard (Issue 6)

- Spans: genai.embeddings.generate, genai.vectors.upsert, genai.vectors.search, genai.vectors.reconcile
- Common attributes: model, dimensions, provider, indexName, entityType, schema, name, k, resultCount, durationMs, tokenUsage.prompt, tokenUsage.completion
- Metrics (counters/histograms): genai.embeddings.generated, genai.vectors.upserted, genai.vectors.search.requests, genai.vectors.search.duration.ms, genai.vectors.reconcile.actions

### 12.21 Runtime provisioning of indexes/containers (Issue 7)

- Configuration: VectorIndex.ProvisionIfMissing (bool, default false). When true, runtime attempts to create minimal resources if not found.
- Azure AI Search: create index with fields: Id (key), Text (searchable), Model/EntityType/Schema/Name (filterable), Embedding (vector, cosine), HNSW defaults. No custom analyzers.
- Cosmos NoSQL: create database if missing; on the Entities container (SemanticModelRepository.CosmosDb.EntitiesContainerName), ensure vector policy and vector index exist for the configured VectorPath (and IndexType/DistanceFunction). Idempotent creation; skip if exists. Do not create a separate vectors container.
- Safety: Log a clear warning when creating resources; failures surface as errors. Intended for dev/test and controlled environments; infra-as-code remains the primary provisioning mechanism.

## 13. Remaining high‑impact design issues (for prioritization)

| # | Issue | Risk | Recommended fix |
|---:|---|---|---|
| 1 | Domain DTOs annotated with provider attributes | Provider lock-in; DIP violation | Move annotated DTOs to Infrastructure.Adapter; keep domain DTOs pure; add mappers |
| 2 | Retry/backoff hard-coded | Untunable; brittle tests | Introduce IRetryPolicy (Polly), bind from settings; inject into writers/generators |
| 3 | Keys/partition composition scattered | Inconsistent IDs; migration issues | Centralize EntityKeyBuilder with sanitize/normalize and max-length checks |
| 4 | Hybrid search toggle only | Inflexible ranking | Add IHybridSearchStrategy with weights and rank fusion; default per provider |
| 5 | Integrity drift (local floats vs external index) | Stale results | Add reconcile-index CLI to compare ContentHash and sync/upsert as needed |
| 6 | Telemetry names not standardized | Observability drift | Define canonical span/metric names and attributes; enforce in code review |
| 7 | Provisioning gap for dev | Onboarding friction | Add dev-only IndexBootstrapper to create minimal indexes/containers when missing |
| 8 | Text pipeline not strongly versioned | Spurious re-embedding | Formalize TextBuilder contract; include version in EmbeddingMetadata.Version |

## Diagram: End-to-end flow

```mermaid
flowchart LR
  A[CLI: generate-vectors] --> B[VectorOrchestrator (Facade)]
  B --> C[EmbeddingGenerator (SK)]
  B --> D[VectorRecordMapper (Adapter)]
  B --> E[Persistence Sync]

  subgraph Repo Strategy
    E --> F1[Local Disk\nentity.json + metadata]
    E --> F2[Azure Blob\nentity.json + metadata]
    E --> F3[Cosmos DB\nNoSQL doc + vector index]
  end

  B --> G[VectorIndexProvider (Strategy)]
  G --> H1[CosmosDB Connector]
  G --> H2[Azure AI Search Connector]
  G --> H3[InMemory Connector]

  C --> G
  D --> G
```

## 14. Repository compatibility and conflict resolutions (finalized)

This section codifies the concrete resolutions identified during review to ensure the spec is fully actionable against the current repository implementation.

### 14.1 Strategy-aware persistence via DTO mapping ✅

Keep `SemanticModelRepository` unchanged. Persistence strategies will map domain entities to storage-specific DTOs to satisfy vector persistence rules per strategy.

| Strategy | Entity payload in storage | Embedding floats in JSON | Serializer | Notes |
|---|---|---:|---|---|
| Local Disk | PersistedEntityDto | ✅ Included | ISecureJsonSerializer | Human-readable JSON, safe serialization (SEC-003) |
| Azure Blob | PersistedEntityDto | ✅ Included | ISecureJsonSerializer | Per-entity blobs; index.json maintained |
| CosmosDB | CosmosEntityDto | ✅ Included (same doc field) | ISecureJsonSerializer | Same container colocation: vectors are stored on the entity documents at the configured VectorPath with container vector policy |

Small flow diagram of write operations by strategy:

```mermaid
flowchart TB
  subgraph Local/Blob
    D1[Domain Entity] --> M1[Persistence Mapper]
    M1 --> P1[PersistedEntityDto (with floats)] --> S1[(JSON on disk/blob)]
  end
  subgraph CosmosDB (Same-container)
    D2[Domain Entity] --> M2[Cosmos Mapper]
    M2 --> P2[CosmosEntityDto (with vector field + metadata)] --> S2[(Cosmos doc in Entities container)]
    Q[Container Vector Policy + Index]:::note
  end

  classDef note fill:#eef,stroke:#99f,color:#003;
```

### 14.2 PersistedEntityDto (Local/Blob) — includes embeddings 📦➡️🧠

For Local Disk and Azure Blob, persist the embedding floats and metadata inside the entity JSON via a storage DTO. Absent fields remain valid for backward compatibility.

```csharp
public sealed class PersistedEntityDto
{
  public required string Schema { get; init; }
  public required string Name { get; init; }
  public string? Description { get; init; }
  public string? SemanticDescription { get; init; }
  public bool NotUsed { get; init; }
  public string? NotUsedReason { get; init; }

  // Domain-specific payload (e.g., columns/indexes for tables)
  public object? DomainSpecific { get; init; }

  public EmbeddingContainer? Embedding { get; init; }

  public sealed class EmbeddingContainer
  {
    public required float[] Vector { get; init; }
    public required EmbeddingMetadata Metadata { get; init; }
  }
}
```

Mapping rules:

- Domain → PersistedEntityDto: copy basic fields; project domain-specific shape into `DomainSpecific` (or structured sub-DTOs), attach `Embedding` when available.
- PersistedEntityDto → Domain: ignore `Embedding` fields when loading (vector orchestration governs embeddings), but preserve non-vector fields.
- JSON remains indented and human-readable (PER-003).

### 14.3 CosmosEntityDto (CosmosDB) — vectors colocated on entity docs ☁️🧩

For CosmosDB, vectors are stored in the SAME container and documents as the entities. Persist the vector array at the configured VectorPath together with metadata.

```csharp
public sealed class CosmosEntityDto<T>
{
  public required string id { get; init; }                 // e.g., "{model}:{type}:{schema}.{name}"
  public required string modelName { get; init; }          // partition key per 12.11
  public required string entityType { get; init; }         // table|view|storedprocedure
  public required string entityName { get; init; }
  public required T data { get; init; }                    // domain entity payload (no duplication of vectors here)

  // Vector and metadata colocated on the entity document
  public float[]? embedding_vector { get; init; }          // matches VectorIndex.CosmosDB.VectorPath
  public EmbeddingMetadata? embeddingMetadata { get; init; }

  public DateTimeOffset createdAt { get; init; } = DateTimeOffset.UtcNow;
}
```

Mapping rules:

- Domain → `T`: domain entity payload as-is (non-vector fields). The vector is written to `embedding_vector` (or the configured path) on the same document.
- Metadata: include `EmbeddingMetadata` for drift detection and reconciliation.
- Queries/Search: use container vector policy + `VectorDistance` queries or compatible SDK abstractions.

### 14.4 LocalDisk secure serialization 🔒

Use the same `ISecureJsonSerializer` used by `AzureBlobPersistenceStrategy` for the `LocalDiskPersistenceStrategy` to meet SEC-003. This ensures safe serialization and optional audit tagging.

Illustrative constructor (strategy wiring may vary in DI):

```csharp
public class LocalDiskPersistenceStrategy : ILocalDiskPersistenceStrategy
{
  private readonly ISecureJsonSerializer _secureJsonSerializer;
  private readonly ILogger<LocalDiskPersistenceStrategy> _logger;

  public LocalDiskPersistenceStrategy(
    ILogger<LocalDiskPersistenceStrategy> logger,
    ISecureJsonSerializer secureJsonSerializer)
  {
    _logger = logger;
    _secureJsonSerializer = secureJsonSerializer;
  }

  // Use _secureJsonSerializer for all JSON writes/reads
}
```

### 14.5 Save semantics and repository impact 🧩

- `SemanticModelRepository` remains unchanged; it orchestrates strategy calls, caching, and concurrency but is agnostic of vector shapes.
- Full-save semantics in strategies are acceptable. Vector flows update:
  - Local/Blob: entity JSON (via `PersistedEntityDto`) includes floats + metadata after generation.
  - CosmosDB: entity docs (via `CosmosEntityDto`) include metadata only; floats live in the vector index managed by SK connector.

### 14.6 Optional supporting components (recommended)

- Centralized key builder (sanitization, max-length, deterministic IDs; see 12.16):

```csharp
public interface IEntityKeyBuilder
{
  string BuildRecordId(string model, string entityType, string schema, string name); // "{model}:{type}:{schema}.{name}"
}
```

- Options validator for fail-fast dimension/model checks (REQ-009, 12.17):

```csharp
public sealed class VectorOptionsValidator : IValidateOptions<VectorIndexOptions>
{
  public ValidateOptionsResult Validate(string? name, VectorIndexOptions options)
  {
    if (options.ExpectedDimensions <= 0)
      return ValidateOptionsResult.Fail("VectorIndex.ExpectedDimensions must be > 0.");
    // Add provider/repo compatibility checks here…
    return ValidateOptionsResult.Success;
  }
}
```
