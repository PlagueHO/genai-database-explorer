---
title: Semantic Model Embeddings & Storage - Technical Documentation
component_path: src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/
version: 1.0
date_created: 2025-08-11
owner: Core Platform
tags: [component, vectors, embeddings, storage, repository]
---

This document explains how embeddings are generated, stored, and searched across repository strategies.

## Overview

- Embeddings are generated post enrichment using the configured embedding model via ISemanticKernelFactory.
- Vector dimension must match the embedding model (validated at runtime).
- Provider-agnostic search API returns top-k hits; SK InMemory is the default local provider.

## Record Contract

Runtime indexing uses `EntityVectorRecord` with Microsoft.Extensions.VectorData attributes:

- Id (VectorStoreKey)
- Content (VectorStoreData)
- Vector (VectorStoreVector(expectedDimension))
- Schema, EntityType, Name, ContentHash (VectorStoreData)

## Persistence by Strategy

### LocalDisk / AzureBlob

- Persisted alongside entity JSON using `PersistedEntityDto`:
  - `data`: Domain entity.
  - `embedding`:
    - `vector`: float[] (human-readable JSON)
    - `metadata`:
      - `modelId`, `dimensions`, `contentHash`, `generatedAt`, `serviceId`, `version`
- External index may also be upserted (e.g., Azure AI Search); local floats remain persisted.

### Cosmos DB

- Documents store metadata only using `CosmosEntityDto<T>`:
  - `data`: Domain entity (no vectors in Cosmos docs)
  - `embedding`: metadata (no `vector` array)
- Vectors are stored in the external index (Cosmos vector connector handles vector storage/indexing).

## Generation & Idempotency

- Text is built from entity structure + enriched descriptions.
- A `contentHash` is computed; unchanged content is skipped unless `overwrite=true`.
- `generatedAt` and `serviceId` track provenance; no secrets are persisted.

## Search

- In-memory tests use SK InMemory vector store (services.AddInMemoryVectorStore()).
- Cloud providers (Azure AI Search, Cosmos NoSQL) are supported via SK connectors.

## Related Files

- Core DTOs: `Repository/DTO/PersistedEntityDto.cs`, `Repository/DTO/CosmosEntityDto.cs`
- Vector record: `SemanticVectors/Records/EntityVectorRecord.cs`
- Orchestration: `SemanticVectors/Orchestration/*`
- CLI: `GenerateVectorsCommandHandler`, `ReconcileIndexCommandHandler`
