# Data Model: API Vector Search Endpoint

**Feature**: 007-api-vector-search
**Date**: 2026-03-01

## Entities

### SearchRequest (API input)

Represents the user's search intent sent as a JSON POST body.

| Field | Type | Required | Default | Constraints |
|-------|------|----------|---------|-------------|
| query | string | Yes | — | Non-empty, non-whitespace, max 2000 characters |
| limit | int | No | 10 | 1–10 (capped at 10) |
| entityTypes | string[] | No | null (all types) | Each element must be one of: "table", "view", "storedProcedure" |

### SearchResultResponse (API output item)

A single matched entity returned in the search response array.

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| entityType | string | No | "Table", "View", or "StoredProcedure" |
| schema | string | No | Database schema name (e.g., "SalesLT") |
| name | string | No | Entity name (e.g., "Customer") |
| description | string | No | Indexed content description |
| score | double | No | Relevance score (0.0–1.0, higher = more relevant) |

### SearchResponse (API output wrapper)

The response returned by the search endpoint.

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| results | SearchResultResponse[] | No | Ranked list of matching entities (may be empty) |
| totalResults | int | No | Count of results returned (0–10) |

## Core Domain Extension

### SemanticModelSearchService — new method

```
SearchAsync(query: string, topK: int, entityTypes: string[]?, cancellationToken) → IReadOnlyList<SemanticModelSearchResult>
```

Flow:
1. Validate inputs (query non-empty, topK > 0)
2. Create VectorInfrastructure from project settings
3. Generate embedding for query text (single embedding call)
4. Search with over-fetched topK (topK × OverFetchMultiplier)
5. Filter by entity types (if specified)
6. Apply minimum score threshold
7. Take topK results
8. Return ranked results

### Minimum Score Threshold

Applied in the SearchAsync unified method after vector search returns raw results.

| Provider | Threshold | Scoring methodology |
|----------|-----------|-------------------|
| InMemory | 0.3 | Cosine similarity (0.0–1.0) |
| CosmosDB | 0.3 | Cosine similarity (0.0–1.0) |
| Azure AI Search | 0.3 | Normalized score (provider-dependent) |

The threshold is applied as a post-filter: `results.Where(r => r.Score >= MinimumScoreThreshold)`.

## Relationships

```
SearchRequest → (API endpoint) → ISemanticModelSearchService.SearchAsync()
                                     ↓
                              IEmbeddingGenerator.GenerateAsync() → query embedding
                                     ↓
                              IVectorSearchService.SearchAsync() → raw results
                                     ↓
                              Filter by entityTypes + score threshold
                                     ↓
                              SemanticModelSearchResult[] → mapped to SearchResultResponse[]
```

## State Transitions

None — search is stateless. No data is created, updated, or deleted.
