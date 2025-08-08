---
goal: Add vector embeddings, indexing, and search across repository strategies (Local, Blob, Cosmos) per spec
version: 1.0
date_created: 2025-08-09
last_updated: 2025-08-09
owner: GenAI Database Explorer Team
status: 'Planned'
tags: [feature, data, vectors, embeddings, search, semantic-kernel, azure, cosmos, ai-search]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This plan implements the Data Vector Embeddings and Indexing Specification, adding embedding generation, provider-aware persistence, vector index upsert/search, and CLI commands (generate-vectors, reconcile-index) while keeping the repository facade unchanged.

## 1. Requirements & Constraints

- REQ-001: Generate embeddings for entities (enriched descriptions + structure)
- REQ-002: Use Microsoft.Extensions.VectorData.Abstractions for record/collection modeling
- REQ-003: Use SK vector-store connectors for Cosmos NoSQL, Azure AI Search, and InMemory
- REQ-004: CLI command generate-vectors (post enrich-model / data-dictionary)
- REQ-005: Local/Blob: persist floats + metadata in entity JSON; may push to external index
- REQ-006: Cosmos: store vectors via Cosmos connector; metadata only in entity docs
- REQ-007: Provider-agnostic search API returning top-k hits
- REQ-008: Idempotent updates via content hash + metadata
- REQ-009: Validate model/dimension/index at startup; fail-fast on mismatch
- REQ-010: Optional hybrid search where supported
- SEC-001: No secrets persisted; use env/managed identity
- SEC-002: Sanitize record keys/IDs
- SEC-003: Safe JSON serialization for Local/Blob
- PER-001: Batch embedding; skip unchanged entities
- PER-002: Search p95 ≤ 500ms (k ≤ 20) for cloud indices
- PER-003: Human-readable floats for Local/Blob
- CON-001: Use ISemanticKernelFactory; embeddings registered via serviceId
- CON-002: Cosmos uses native vector index only (no external index simultaneously)
- CON-003: Local/Blob may use external index; still persist floats locally
- CON-004: Dimension must match embedding model
- GUD/PAT: Strategy + Abstract Factory + Adapter/Mapper + Facade + Template Method/Pipeline

## 2. Implementation Steps

### Implementation Phase 1

- GOAL-001: Establish dependencies, options, DI wiring, and configuration contracts

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-001 | Add NuGet to Core: Microsoft.Extensions.VectorData.Abstractions v9.7.0 in src/GenAIDBExplorer/GenAIDBExplorer.Core/GenAIDBExplorer.Core.csproj |  |  |
| TASK-002 | Add NuGet to Core: Microsoft.SemanticKernel.Connectors.AzureAISearch v1.61.0 |  |  |
| TASK-003 | Add NuGet to Core: Microsoft.SemanticKernel.Connectors.CosmosNoSql v1.61.0 |  |  |
| TASK-004 | Create VectorIndexOptions in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Options/VectorIndexOptions.cs (Provider, CollectionName, PushOnGenerate, ProvisionIfMissing, AllowedForRepository, AzureAISearch, CosmosNoSql, EmbeddingServiceId, ExpectedDimensions, Hybrid) |  |  |
| TASK-005 | Create VectorOptionsValidator in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Options/VectorOptionsValidator.cs implementing IValidateOptions<`VectorIndexOptions`> (REQ-009, 12.17) |  |  |
| TASK-006 | Wire options & services in src/GenAIDBExplorer/GenAIDBExplorer.Console/Extensions/HostBuilderExtensions.cs: Configure<`VectorIndexOptions`>(), AddSingleton<IValidateOptions<`VectorIndexOptions`>, VectorOptionsValidator>() |  |  |
| TASK-007 | Register services in HostBuilderExtensions: IVectorIndexPolicy, IVectorInfrastructureFactory, IVectorRecordMapper, IEmbeddingGenerator, IVectorIndexWriter (provider-specific), IVectorSearchService, IVectorGenerationService, IVectorOrchestrator, IEntityKeyBuilder |  |  |
| TASK-008 | Extend src/GenAIDBExplorer/GenAIDBExplorer.Console/appsettings.json: add VectorIndex section per spec (with placeholders keyed to env vars) |  |  |
| TASK-009 | Update samples/AdventureWorksLT/settings.json with VectorIndex defaults (Provider=Auto; ExpectedDimensions; EmbeddingServiceId="Embeddings") |  |  |

Completion criteria:

- Core compiles with new package references and options types.
- DI validates at startup; misconfiguration produces clear failures.

### Implementation Phase 2

- GOAL-002: Strategy-aware persistence via DTOs; secure serialization for LocalDisk

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-010 | Create PersistedEntityDto in src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/DTO/PersistedEntityDto.cs (Embedding: Vector+EmbeddingMetadata) |  |  |
| TASK-011 | Create CosmosEntityDto<`T`> in src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/DTO/CosmosEntityDto.cs (no floats; metadata only) |  |  |
| TASK-012 | Introduce IStorageEntityMapper in src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/Mappers/IStorageEntityMapper.cs and implementations: LocalBlobEntityMapper.cs and CosmosEntityMapper.cs |  |  |
| TASK-013 | Refactor LocalDiskPersistenceStrategy to use ISecureJsonSerializer for all IO and PersistedEntityDto mapping (SEC-003, REQ-005) |  |  |
| TASK-014 | Refactor AzureBlobPersistenceStrategy to write/read PersistedEntityDto via ISecureJsonSerializer (REQ-005) |  |  |
| TASK-015 | Refactor CosmosPersistenceStrategy to use CosmosEntityDto<`T`> (metadata only) and ensure no floats are written to Cosmos documents (REQ-006) |  |  |

Completion criteria:

- Local/Blob entity JSON contains floats + metadata.
- Cosmos entity docs contain metadata only; no float arrays present.
- All strategies pass existing repository unit tests.

### Implementation Phase 3

- GOAL-003: Vector infrastructure (policy, factory, mappers, generator, writers, search)

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-016 | Add IEntityKeyBuilder in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Keys/IEntityKeyBuilder.cs and EntityKeyBuilder.cs with sanitize/normalize (12.16) |  |  |
| TASK-017 | Add IVectorIndexPolicy and VectorIndexPolicy.cs in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Policy/ (Auto resolution, repo compatibility checks per 12.2, 12.17) |  |  |
| TASK-018 | Add IVectorInfrastructureFactory and VectorInfrastructureFactory.cs in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Infrastructure/ (build SK collection and embedding generator based on options/provider) |  |  |
| TASK-019 | Add EntityVectorRecord in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Records/EntityVectorRecord.cs with VectorData attributes per spec |  |  |
| TASK-020 | Add IVectorRecordMapper and VectorRecordMapper.cs in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Mapping/ (BuildEntityText + ToRecord) |  |  |
| TASK-021 | Add IEmbeddingGenerator and SemanticKernelEmbeddingGenerator.cs in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Embeddings/ (uses ISemanticKernelFactory with EmbeddingServiceId) |  |  |
| TASK-022 | Add IVectorIndexWriter abstractions and provider implementations: AzureAISearchIndexWriter.cs, CosmosNoSqlIndexWriter.cs, InMemoryIndexWriter.cs in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Indexing/ |  |  |
| TASK-023 | Add IVectorSearchService with provider implementations in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Search/ |  |  |

Completion criteria:

- Factory returns correctly configured collection and generator for chosen provider.
- Writers upsert records; search returns results via the selected provider.

### Implementation Phase 4

- GOAL-004: Orchestration pipeline and CLI commands

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-024 | Add IVectorGenerationService and VectorGenerationService.cs in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Orchestration/ (batching, content hash, overwrite behavior) |  |  |
| TASK-025 | Add IVectorOrchestrator and VectorOrchestrator.cs in src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Orchestration/ (sequence: select → build text → embed → persist → upsert) |  |  |
| TASK-026 | Create GenerateVectorsCommandHandler.cs in src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/ (options per 12.5; integrate with OutputService and Project) |  |  |
| TASK-027 | Create ReconcileIndexCommandHandler.cs in src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/ (per 12.19) |  |  |
| TASK-028 | Register new command handlers in HostBuilderExtensions and wire to Program.cs (Command setup pattern used by existing handlers) |  |  |

Completion criteria:

- CLI generate-vectors runs end-to-end for Local strategy with InMemory by default.
- Reconcile-index command reports drift and can upsert when not in dry-run.

### Implementation Phase 5

- GOAL-005: Tests (unit + in-memory e2e) and telemetry

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-029 | Unit tests (Core): Key builder, policy resolution, options validator, record mapper, embedding generator wrapper (mock SK), generation skip on unchanged content |  |  |
| TASK-030 | Unit tests (Repository): Local/Blob write floats; Cosmos metadata-only (DTO mapping) |  |  |
| TASK-031 | InMemory end-to-end tests: generate + search deterministic flows (no Azure deps) |  |  |
| TASK-032 | Pester CLI test: run generate-vectors on samples/AdventureWorksLT and assert updated artifacts/log output |  |  |
| TASK-033 | Telemetry: add spans and counters named in spec (12.7, 12.20) using existing IPerformanceMonitor hooks; document how to enable OTel later |  |  |

Completion criteria:

- New tests pass locally and in CI; coverage ≥ 80% on new code.
- CLI Pester test produces NUnitXml result without failures.

### Implementation Phase 6

- GOAL-006: Documentation, samples, and infra alignment

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-034 | Update docs/cli/README.md adding generate-vectors & reconcile-index commands with examples |  |  |
| TASK-035 | Update docs/components/semantic-model-documentation.md with embedding fields for Local/Blob and metadata for Cosmos |  |  |
| TASK-036 | Ensure infra/main.bicep references include AI Search and Cosmos guidance (link to existing infra/ and spec 12.21) |  |  |

Completion criteria:

- Docs updated; samples show VectorIndex in settings; infra guidance aligns with runtime provisioning rules.

## 3. Alternatives

- ALT-001: Store vectors in separate local files for Local/Blob instead of embedding in entity JSON. Rejected: reduces portability/readability; complicates repository changes.
- ALT-002: Add vector fields directly to domain entities. Rejected: violates decoupling (GUD-002) and complicates Cosmos duplication concerns (REQ-006).

## 4. Dependencies

- DEP-001: NuGet Microsoft.SemanticKernel (already in Core) — ensure connectors version matches (1.61.0)
- DEP-002: NuGet Microsoft.SemanticKernel.Connectors.AzureAISearch (1.61.0)
- DEP-003: NuGet Microsoft.SemanticKernel.Connectors.CosmosNoSql (1.61.0)
- DEP-004: NuGet Microsoft.Extensions.VectorData.Abstractions (9.7.0)
- DEP-005: Azure credentials via env/managed identity (when using cloud providers)

## 5. Files

- FILE-001: src/GenAIDBExplorer/GenAIDBExplorer.Core/GenAIDBExplorer.Core.csproj — add package refs
- FILE-002: src/GenAIDBExplorer/GenAIDBExplorer.Console/Extensions/HostBuilderExtensions.cs — register options and services
- FILE-003: src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/GenerateVectorsCommandHandler.cs — new CLI command
- FILE-004: src/GenAIDBExplorer/GenAIDBExplorer.Console/CommandHandlers/ReconcileIndexCommandHandler.cs — new CLI command
- FILE-005: src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/DTO/PersistedEntityDto.cs — Local/Blob storage DTO
- FILE-006: src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/DTO/CosmosEntityDto.cs — Cosmos storage DTO
- FILE-007: src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/Mappers/IStorageEntityMapper.cs — strategy mappers + implementations
- FILE-008: src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/LocalDiskPersistenceStrategy.cs — refactor to secure serializer + DTO
- FILE-009: src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/AzureBlobPersistenceStrategy.cs — refactor to DTO
- FILE-010: src/GenAIDBExplorer/GenAIDBExplorer.Core/Repository/CosmosPersistenceStrategy.cs — refactor to metadata-only DTO
- FILE-011: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Options/VectorIndexOptions.cs — vector options
- FILE-012: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Options/VectorOptionsValidator.cs — fail-fast validator
- FILE-013: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Keys/IEntityKeyBuilder.cs, EntityKeyBuilder.cs — key builder
- FILE-014: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Policy/IVectorIndexPolicy.cs, VectorIndexPolicy.cs — policy
- FILE-015: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Infrastructure/IVectorInfrastructureFactory.cs, VectorInfrastructureFactory.cs — factory
- FILE-016: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Records/EntityVectorRecord.cs — record contract
- FILE-017: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Mapping/IVectorRecordMapper.cs, VectorRecordMapper.cs — mapper
- FILE-018: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Embeddings/IEmbeddingGenerator.cs, SemanticKernelEmbeddingGenerator.cs — embedding wrapper
- FILE-019: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Indexing/IVectorIndexWriter.cs + AzureAISearchIndexWriter.cs + CosmosNoSqlIndexWriter.cs + InMemoryIndexWriter.cs — writers
- FILE-020: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Search/IVectorSearchService.cs + provider impls — search
- FILE-021: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Orchestration/IVectorGenerationService.cs, VectorGenerationService.cs, IVectorOrchestrator.cs, VectorOrchestrator.cs — orchestration
- FILE-022: tests for Core + Console in src/GenAIDBExplorer/Tests/Unit/… and Tests/Integration/Console.Integration.Tests.ps1 — add/extend

## 6. Testing

- TEST-001: Policy resolution — Given repo strategy + Provider=Auto, returns expected provider per table in 12.2
- TEST-002: Options validation — dimension mismatch → fail-fast; Cosmos+external → fail-fast
- TEST-003: Record mapping — BuildEntityText contains schema/name/semantics; ToRecord sets key and metadata
- TEST-004: Generation idempotency — unchanged content hash → skip; overwrite=true → re-embed
- TEST-005: Local/Blob persistence — floats + metadata present in JSON
- TEST-006: Cosmos persistence — metadata present; floats absent
- TEST-007: InMemory e2e — generate-vectors + search returns deterministic top-k
- TEST-008: CLI Pester — generate-vectors updates sample project and writes summary JSON

## 7. Risks & Assumptions

- RISK-001: Connector package rename drift; pin to versions listed and align with Microsoft.SemanticKernel 1.61.0
- RISK-002: Dimension mismatch across models; mitigated by startup validation and metadata checks
- RISK-003: Large JSON size for Local/Blob; acceptable in v1 (PER-003); consider quantization later
- RISK-004: Cosmos RU spikes during upserts; batch/sleep if throttled (retry policy in v1.1)
- ASSUMPTION-001: Managed identity or env credentials available when using Azure providers
- ASSUMPTION-002: Existing repository save/load semantics remain unchanged

## 8. Related Specifications / Further Reading

- spec/spec-data-vector-embeddings-and-indexing.md
- docs/cli/README.md (to be updated)
- Microsoft Learn: SK Vector Store connectors (Azure AI Search, Cosmos NoSQL, InMemory)
