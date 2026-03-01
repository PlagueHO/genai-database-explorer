# Feature Specification: API Vector Search Endpoint

**Feature Branch**: `007-api-vector-search`
**Created**: 2026-03-01
**Status**: Draft
**Input**: User description: "Add the search API endpoint that will allow searching of the semantic model entities using vector similarity search. It should return zero or more matching entities (reranked appropriately). It should have a limited number of entities that are returned (no more than 10)."

## Clarifications

### Session 2026-03-01

- Q: Should the search endpoint filter out low-relevance results below a minimum similarity score, or always return top-K regardless of match quality? → A: Apply a default minimum score threshold. Align threshold behavior to the scoring methodology of the active vector index provider (Azure AI Search scoring, InMemory cosine similarity, CosmosDB similarity) so that only meaningfully relevant results are returned.
- Q: How should cross-type ranking work when searching across multiple entity types? → A: Entity type filter is optional. When provided, search only the specified types. When omitted, search all entity types, merge all results, and re-sort by relevance score into a single unified ranked list.
- Q: Should cross-type merging logic live in the API layer or in Core? → A: Add a unified search method to the Core `ISemanticModelSearchService` that handles cross-type search and merge internally. The API endpoint remains a thin mapping layer, consistent with the project's existing architecture.
- Q: How should the endpoint detect and communicate search unavailability (e.g., no embeddings generated)? → A: Treat an empty vector store as normal — return zero results. Only surface explicit infrastructure failures (connection errors, missing configuration) as errors. No proactive "embeddings not generated" detection.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Search Entities by Natural Language (Priority: P1)

A frontend developer or API consumer sends a natural language query (e.g., "customer orders and shipping addresses") to the search endpoint and receives a ranked list of matching semantic model entities (tables, views, stored procedures) ordered by relevance. The response includes entity metadata and a relevance score so the consumer can present meaningful results to their users.

**Why this priority**: This is the core value proposition — exposing the existing vector search capability through the REST API so that any HTTP client (frontend, integration, third-party tool) can discover relevant database entities using natural language, without needing CLI access.

**Independent Test**: Can be fully tested by sending a POST request with a query string and verifying a ranked list of entity summaries is returned. Delivers immediate value for the frontend semantic explorer and any API consumer.

**Acceptance Scenarios**:

1. **Given** vector embeddings have been generated for the semantic model, **When** a user sends a POST request with a natural language query, **Then** the system returns a JSON array of matching entities ranked by relevance score (highest first), each including entity type, schema, name, description, and score.
1. **Given** vector embeddings have been generated, **When** a user sends a query that closely matches a specific table's description, **Then** that table appears with a high relevance score near the top of the results.
1. **Given** vector embeddings have been generated, **When** a user sends a query with no close matches, **Then** the system returns an empty array with no error.
1. **Given** vector embeddings have been generated, **When** results fall below the minimum relevance score threshold for the active provider, **Then** those low-scoring results are excluded even if the requested limit has not been reached.

---

### User Story 2 - Limit Search Results (Priority: P1)

An API consumer specifies the maximum number of results to return (up to a system-enforced cap of 10). When no limit is specified, a sensible default is used. The system never returns more than 10 results regardless of the requested limit.

**Why this priority**: Result limiting is essential for usable search — returning unbounded results degrades performance and user experience. The hard cap of 10 is a core requirement.

**Independent Test**: Can be tested by sending queries with different limit values and verifying the response never exceeds the requested limit or the system maximum of 10.

**Acceptance Scenarios**:

1. **Given** many entities match a query, **When** the user requests a limit of 5, **Then** at most 5 results are returned.
1. **Given** many entities match a query, **When** the user requests a limit of 15 (above the cap), **Then** at most 10 results are returned.
1. **Given** many entities match a query, **When** the user does not specify a limit, **Then** at most the default number of results (10) are returned.
1. **Given** fewer than the requested limit of entities match, **When** the search completes, **Then** only the matching entities are returned (zero or more, up to the limit).

---

### User Story 3 - Filter Search by Entity Type (Priority: P2)

An API consumer optionally specifies one or more entity types (tables, views, stored procedures) to narrow the search. When no entity type filter is provided, the search spans all entity types.

**Why this priority**: Filtering by entity type is a natural refinement for users who know they're looking for a specific kind of database object, but the search is still fully functional without it.

**Independent Test**: Can be tested by sending queries with and without entity type filters and verifying only the requested entity types appear in filtered results.

**Acceptance Scenarios**:

1. **Given** the user specifies entity type "table", **When** the search completes, **Then** only table entities appear in the results.
1. **Given** the user specifies entity types "table" and "view", **When** the search completes, **Then** only table and view entities appear in the results.
1. **Given** the user does not specify any entity type filter, **When** the search completes, **Then** results may include tables, views, and stored procedures.

---

### User Story 4 - Graceful Handling of Unavailable Search (Priority: P2)

When vector embeddings have not been generated or the vector search infrastructure is unavailable, the search endpoint returns a clear, informative error rather than crashing or returning misleading results.

**Why this priority**: Robustness is important for production readiness. Users and integrations need clear feedback when search is not available.

**Independent Test**: Can be tested by calling the search endpoint when no embeddings exist and verifying a descriptive error response is returned.

**Acceptance Scenarios**:

1. **Given** no vector embeddings have been generated, **When** a user sends a search query, **Then** the system returns an empty result set (zero results), not an error.
1. **Given** the semantic model is not loaded, **When** a user sends a search query, **Then** the system returns a service unavailable error consistent with existing API error patterns.
1. **Given** the vector search infrastructure has a connection or configuration failure, **When** a user sends a search query, **Then** the system returns an appropriate error describing the infrastructure issue.

---

### Edge Cases

- What happens when the query text is empty or only whitespace? The system rejects the request with a validation error.
- What happens when the query text is extremely long? The system accepts it up to a reasonable length (the embedding model truncates as needed) but rejects excessively long inputs to prevent abuse.
- What happens when the limit is zero or negative? The system rejects the request with a validation error.
- What happens when the entity type filter contains an unrecognized type? The system rejects the request with a validation error listing valid entity types.
- What happens when many entities match but all have low relevance scores? The system applies the provider-aligned minimum score threshold and returns only results above it (possibly zero results).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose a search endpoint accessible via HTTP POST that accepts a natural language query and returns matching semantic model entities.
- **FR-002**: System MUST return results ranked by relevance score, with the most relevant entities first.
- **FR-003**: System MUST enforce a hard maximum of 10 results per search request, regardless of the requested limit.
- **FR-004**: System MUST support an optional limit parameter that defaults to 10 and is capped at 10.
- **FR-005**: System MUST support an optional entity type filter parameter accepting one or more of: "table", "view", "storedProcedure".
- **FR-006**: When no entity type filter is specified, system MUST search across all entity types (tables, views, stored procedures), merge the results, and return them in a single list sorted by relevance score (highest first).
- **FR-007**: Each result MUST include: entity type, schema name, entity name, description, and a relevance score.
- **FR-008**: System MUST return an empty result set (not an error) when no entities match the query.
- **FR-013**: System MUST apply a minimum relevance score threshold, aligned to the scoring methodology of the active vector index provider (Azure AI Search, InMemory cosine similarity, CosmosDB similarity), filtering out results that fall below the threshold.
- **FR-014**: When all candidate results fall below the minimum score threshold, the system MUST return an empty result set.
- **FR-009**: System MUST return an empty result set when no embeddings exist (not an error). System MUST return an appropriate error only for infrastructure failures (e.g., connection errors, missing configuration, model not loaded).
- **FR-010**: System MUST validate the query input — rejecting empty/whitespace-only queries with a clear error.
- **FR-011**: System MUST reject invalid entity type filter values with a validation error.
- **FR-012**: The search endpoint MUST follow the existing API conventions (JSON responses, camelCase naming, RFC 9457 Problem Details for errors, consistent URL patterns).

### Key Entities

- **Search Request**: Represents the user's search intent — contains a natural language query string, an optional result limit, and an optional entity type filter.
- **Search Result**: A single matched entity — contains entity type, schema name, entity name, descriptive content, and a relevance score indicating how well it matches the query.
- **Search Response**: The collection of zero or more search results returned to the caller.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can discover relevant database entities by describing what they're looking for in natural language, receiving ranked results within a single request-response cycle.
- **SC-002**: The search endpoint returns at most 10 results per request, ensuring concise and actionable responses.
- **SC-003**: Results are ranked so that the most semantically relevant entity appears first at least 80% of the time when tested against a known set of queries and expected top results.
- **SC-004**: The endpoint returns correct validation errors for malformed requests (empty queries, invalid entity types, out-of-range limits) with no server errors.
- **SC-005**: The endpoint provides clear, actionable error messages when search is unavailable, enabling consumers to inform their users appropriately.
- **SC-006**: The endpoint follows the same conventions as existing API endpoints, requiring no special client-side handling beyond standard JSON REST patterns.

## Assumptions

- Vector embeddings have already been generated for the semantic model via the existing `generate-vectors` CLI command or equivalent process before search is used.
- The existing `ISemanticModelSearchService` in Core will be extended with a unified search method for cross-type search and merge. The API endpoint will be a thin layer delegating to this Core service.
- The embedding model used for query embedding is the same model used to generate the entity embeddings (consistency is managed by project configuration).
- The API will reuse the existing vector search infrastructure (InMemory, CosmosDB, Azure AI Search) based on project settings, without requiring a separate search backend.
- Authentication and authorization for the search endpoint follow the same patterns as the existing API endpoints (currently unauthenticated for development).

## Dependencies

- Existing `ISemanticModelSearchService` in Core (provides `SearchTablesAsync`, `SearchViewsAsync`, `SearchStoredProceduresAsync`).
- Existing `IEmbeddingGenerator` and `IVectorSearchService` infrastructure for generating query embeddings and performing similarity search.
- Vector search operates via `ISemanticModelSearchService` (which wraps `IVectorSearchService` and `IEmbeddingGenerator`), not via `ISemanticModelCacheService`. The semantic model cache is used by other entity CRUD endpoints but is not in the search request path.
- Vector embeddings must have been generated for the semantic model entities (via `generate-vectors` command).
