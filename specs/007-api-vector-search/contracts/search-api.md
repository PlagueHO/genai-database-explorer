# API Contract: Search Endpoint

**Feature**: 007-api-vector-search
**Date**: 2026-03-01

## POST /api/search

Performs a natural language vector similarity search across semantic model entities.

### Request

**Method**: POST
**Content-Type**: application/json

```json
{
  "query": "string (required, 1-2000 chars, non-whitespace)",
  "limit": "integer (optional, default: 10, range: 1-10)",
  "entityTypes": ["string"] // optional, subset of ["table", "view", "storedProcedure"]
}
```

#### Request Fields

| Field | Type | Required | Default | Constraints |
|-------|------|----------|---------|-------------|
| query | string | Yes | — | Non-empty, non-whitespace-only, max 2000 characters |
| limit | integer | No | 10 | 1–10; values above 10 are clamped to 10 |
| entityTypes | string[] | No | null (all types) | Each must be `"table"`, `"view"`, or `"storedProcedure"` |

### Responses

#### 200 OK — Successful search

Returns zero or more ranked results. An empty results array is a valid (non-error) response.

```json
{
  "results": [
    {
      "entityType": "Table",
      "schema": "SalesLT",
      "name": "Customer",
      "description": "Contains customer information including names and contact details.",
      "score": 0.87
    },
    {
      "entityType": "View",
      "schema": "SalesLT",
      "name": "vGetAllCategories",
      "description": "Returns all product categories in a hierarchical view.",
      "score": 0.72
    }
  ],
  "totalResults": 2
}
```

**Response Fields**:

| Field | Type | Description |
|-------|------|-------------|
| results | SearchResultResponse[] | Ranked list of matching entities (highest score first) |
| totalResults | int | Number of results returned (0–10) |

**SearchResultResponse**:

| Field | Type | Description |
|-------|------|-------------|
| entityType | string | `"Table"`, `"View"`, or `"StoredProcedure"` |
| schema | string | Database schema name |
| name | string | Entity name |
| description | string | Entity content/description used for the search |
| score | double | Relevance score (0.0–1.0, higher is more relevant) |

#### 400 Bad Request — Validation error

Returned when request body fails validation. Uses RFC 9457 Problem Details.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "detail": "Query must not be empty.",
  "status": 400
}
```

**Validation rules triggering 400**:
- `query` is null, empty, or whitespace-only
- `query` exceeds 2000 characters
- `limit` is less than 1
- `entityTypes` contains an unrecognized value

#### 503 Service Unavailable — Infrastructure failure

Returned when the semantic model is not loaded or there is an infrastructure failure.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.4",
  "title": "Service Unavailable",
  "detail": "The semantic model search service is not currently available.",
  "status": 503
}
```

### Examples

#### Search all entity types (default)

```
POST /api/search
Content-Type: application/json

{
  "query": "customer orders and shipping addresses"
}
```

#### Search with limit and type filter

```
POST /api/search
Content-Type: application/json

{
  "query": "product inventory",
  "limit": 5,
  "entityTypes": ["table", "view"]
}
```

#### Response with no matches

```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "results": [],
  "totalResults": 0
}
```

### Conventions

- **JSON naming**: camelCase (consistent with all existing API endpoints)
- **Error format**: RFC 9457 Problem Details (consistent with all existing API error responses)
- **OpenAPI metadata**: `.WithName("SearchEntities")`, `.WithDescription("Search semantic model entities using natural language")`, `.WithTags("Search")`
- **Produces declarations**: `.Produces<SearchResponse>()`, `.ProducesProblem(400)`, `.ProducesProblem(503)`
