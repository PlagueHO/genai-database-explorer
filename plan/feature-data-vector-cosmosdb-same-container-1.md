---
goal: Align vector index code with CosmosDB same-container spec (provider rename + options shape)
version: 1.0
date_created: 2025-08-12
last_updated: 2025-08-12
owner: GenAI Database Explorer Team
status: 'Completed'
tags: [feature, data, vectors, cosmosdb, config, validation]
---

# Introduction

![Status: Completed](https://img.shields.io/badge/status-Completed-green)

Implement only the code changes needed to match the updated spec for CosmosDB vector storage: when the semantic model persistence is CosmosDB, vectors must be stored on the same entity documents (same container), and the vector provider is named "CosmosDB" (not "CosmosNoSql"). No extra Cosmos connection settings are required in VectorIndex. This plan updates provider naming, options shape, validation, policy logic, and unit tests.

## 1. Requirements & Constraints

- REQ-006: For CosmosDB strategy, store vectors in the SAME CosmosDB container and documents as the semantic model entities; do not use a separate vector container.
- CON-002: CosmosDB must use a container-level vector policy on the Entities container; external vector indices are not supported concurrently.
- REQ-003: Use SK vector-store connector for CosmosDB.
- CFG-001: VectorIndex provider name must be "CosmosDB" (not "CosmosNoSql").
- CFG-002: VectorIndex must not request Cosmos connection settings for CosmosDB provider; only vector-specific fields are allowed: VectorPath, DistanceFunction, IndexType.
- VAL-001: Validate allowed values for DistanceFunction: [cosine, dotproduct, euclidean].
- VAL-002: Validate allowed values for IndexType: [diskANN, quantizedFlat, flat].
- POL-001: Auto policy must choose CosmosDB provider when repository strategy is Cosmos (semantic model persisted in CosmosDB).
- TEST-001: Update unit tests to expect provider "CosmosDB" and new validation semantics; remove expectations for CosmosNoSql-specific connection settings.

## 2. Implementation Steps

### Implementation Phase 1

- GOAL-001: Rename provider and options shape in code (settings models and options classes)

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-001 | Edit `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Options/VectorIndexOptions.cs`: rename inner class `CosmosNoSqlOptions` -> `CosmosDBOptions` and property `CosmosNoSql` -> `CosmosDB`. Replace fields with `string? VectorPath`, `string? DistanceFunction`, `string? IndexType`. Update XML doc to: "Valid values: Auto, InMemory, AzureAISearch, CosmosDB." | ✅ | 2025-08-12 |
| TASK-002 | Edit `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/VectorIndexSettings.cs`: rename nested `CosmosNoSqlSettings` -> `CosmosDBSettings` and property `CosmosNoSql` -> `CosmosDB`. Replace fields with `string? VectorPath`, `string? DistanceFunction`, `string? IndexType`. Update Provider comment to include CosmosDB instead of CosmosNoSql. | ✅ | 2025-08-12 |

### Implementation Phase 2

- GOAL-002: Update validation and policy logic to enforce same-container CosmosDB rules

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-003 | Edit `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Options/VectorOptionsValidator.cs`: change `validProviders` set to `{"Auto","InMemory","AzureAISearch","CosmosDB"}`. Remove checks for `CosmosNoSql.AccountEndpoint/Database/Container`. Add CosmosDB checks: `VectorPath` required and non-empty (recommend starts with "/"); `DistanceFunction` in {cosine, dotproduct, euclidean}; `IndexType` in {diskANN, quantizedFlat, flat}. | ✅ | 2025-08-12 |
| TASK-004 | Edit `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Policy/VectorIndexPolicy.cs`: in `ResolveProvider`, when `repositoryStrategy.Equals("CosmosDb", OrdinalIgnoreCase)`, return `"CosmosDB"`. In `Validate`, require provider `CosmosDB` when `repositoryStrategy.Equals("CosmosDb", ...)`; update exception text to: "Cosmos persistence requires CosmosDB vector provider (CON-002)." | ✅ | 2025-08-12 |

### Implementation Phase 3

- GOAL-003: Update unit tests to new provider name and validation rules

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-005 | Edit `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/VectorEmbedding/Policy/VectorIndexPolicyTests.cs`: replace expected provider string `"CosmosNoSql"` with `"CosmosDB"`; update validation assertions to expect message containing "CosmosDB" instead of "CosmosNoSql". | ✅ | 2025-08-12 |
| TASK-006 | Add/adjust validator tests under `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/` to assert: (a) CosmosDB with missing `VectorPath` fails; (b) invalid `DistanceFunction` fails; (c) invalid `IndexType` fails; (d) valid values pass; (e) AzureAISearch path unchanged. | ✅ | 2025-08-12 |

### Implementation Phase 4

- GOAL-004: Config template alignment and verification

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-007 | Verify `src/GenAIDBExplorer/GenAIDBExplorer.Core/DefaultProject/settings.json` uses `VectorIndex.CosmosDB` with `VectorPath/DistanceFunction/IndexType` and provider comments list CosmosDB. Adjust if needed. | ✅ | 2025-08-12 |
| TASK-008 | Run build + unit tests via workspace tasks; ensure green. If failures reference old provider name, update references accordingly. | ✅ | 2025-08-12 |

## 3. Alternatives

- ALT-001: Keep `CosmosNoSql` provider name and map internally to CosmosDB semantics. Rejected to avoid user confusion and align with spec naming.
- ALT-002: Require Cosmos connection fields under `VectorIndex` for CosmosDB. Rejected because same-container design uses existing repository connection; duplicate connection settings are unnecessary and error-prone.

## 4. Dependencies

- DEP-001: Existing Semantic Model repository configuration for Cosmos remains under `SemanticModelRepository.CosmosDb` and is reused by same-container vector operations.
- DEP-002: SK CosmosDB connector availability at runtime (no code changes required here).

## 5. Files

- FILE-001: `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Options/VectorIndexOptions.cs`
- FILE-002: `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/VectorIndexSettings.cs`
- FILE-003: `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Options/VectorOptionsValidator.cs`
- FILE-004: `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Policy/VectorIndexPolicy.cs`
- FILE-005: `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/VectorEmbedding/Policy/VectorIndexPolicyTests.cs`
- FILE-006: `src/GenAIDBExplorer/GenAIDBExplorer.Core/DefaultProject/settings.json` (verify only)

## 6. Testing

- TEST-001: Policy Auto resolves to `CosmosDB` when repository strategy is `Cosmos`.
- TEST-002: Policy Validate throws when repository strategy is `CosmosDb` and provider != `CosmosDB`.
- TEST-003: Validator fails when CosmosDB.VectorPath is null/empty.
- TEST-004: Validator fails when CosmosDB.DistanceFunction ∉ {cosine, dotproduct, euclidean}.
- TEST-005: Validator fails when CosmosDB.IndexType ∉ {diskANN, quantizedFlat, flat}.
- TEST-006: Validator passes for valid CosmosDB values and AzureAISearch remains unchanged.

## 7. Risks & Assumptions

- RISK-001: Renaming options/properties breaks existing configs referencing `CosmosNoSql`. Mitigation: communicate change in release notes; consider backward-compat shim in a future task.
- ASSUMPTION-001: Repository strategy string remains `"CosmosDb"` elsewhere in the codebase; this plan does not rename the repository strategy itself, only the vector provider/options.

## 8. Related Specifications / Further Reading

- Spec: `spec/spec-data-vector-embeddings-and-indexing.md` (CosmosDB same-container; provider name and config shape)
- Default settings template: `src/GenAIDBExplorer/GenAIDBExplorer.Core/DefaultProject/settings.json`
- SK CosmosDB NoSQL connector docs: [Semantic Kernel CosmosDB NoSQL connector](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/azure-cosmosdb-nosql-connector)
