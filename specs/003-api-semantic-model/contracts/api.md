# API Contracts: REST API for Semantic Model Repository

**Feature**: 003-api-semantic-model
**Date**: 2026-02-23
**Base Path**: `/api`

## Endpoints

### Model

| Method | Path | Description | Request Body | Response | Status Codes |
|--------|------|-------------|-------------|----------|--------------|
| GET | `/api/model` | Retrieve semantic model summary | — | `SemanticModelSummaryResponse` | 200, 503 |
| POST | `/api/model/reload` | Reload model from persistence | — | `SemanticModelSummaryResponse` | 200, 503 |

### Tables

| Method | Path | Description | Request Body | Response | Status Codes |
|--------|------|-------------|-------------|----------|--------------|
| GET | `/api/tables?offset=0&limit=50` | List tables (paginated) | — | `PaginatedResponse<EntitySummaryResponse>` | 200, 503 |
| GET | `/api/tables/{schema}/{name}` | Get table details | — | `TableDetailResponse` | 200, 404, 503 |
| PATCH | `/api/tables/{schema}/{name}` | Update table descriptions | `UpdateEntityDescriptionRequest` | `TableDetailResponse` | 200, 400, 404, 503 |

### Views

| Method | Path | Description | Request Body | Response | Status Codes |
|--------|------|-------------|-------------|----------|--------------|
| GET | `/api/views?offset=0&limit=50` | List views (paginated) | — | `PaginatedResponse<EntitySummaryResponse>` | 200, 503 |
| GET | `/api/views/{schema}/{name}` | Get view details | — | `ViewDetailResponse` | 200, 404, 503 |
| PATCH | `/api/views/{schema}/{name}` | Update view descriptions | `UpdateEntityDescriptionRequest` | `ViewDetailResponse` | 200, 400, 404, 503 |

### Stored Procedures

| Method | Path | Description | Request Body | Response | Status Codes |
|--------|------|-------------|-------------|----------|--------------|
| GET | `/api/stored-procedures?offset=0&limit=50` | List stored procedures (paginated) | — | `PaginatedResponse<EntitySummaryResponse>` | 200, 503 |
| GET | `/api/stored-procedures/{schema}/{name}` | Get stored procedure details | — | `StoredProcedureDetailResponse` | 200, 404, 503 |
| PATCH | `/api/stored-procedures/{schema}/{name}` | Update stored procedure descriptions | `UpdateEntityDescriptionRequest` | `StoredProcedureDetailResponse` | 200, 400, 404, 503 |

### Project

| Method | Path | Description | Request Body | Response | Status Codes |
|--------|------|-------------|-------------|----------|--------------|
| GET | `/api/project` | Get project configuration info | — | `ProjectInfoResponse` | 200, 503 |

### Health (provided by ServiceDefaults)

| Method | Path | Description | Response | Status Codes |
|--------|------|-------------|----------|--------------|
| GET | `/health` | Readiness check (all health checks pass) | Health status | 200, 503 |
| GET | `/alive` | Liveness check (app is responsive) | Health status | 200, 503 |

## Query Parameters

### Pagination (list endpoints)

| Parameter | Type | Default | Min | Max | Description |
|-----------|------|---------|-----|-----|-------------|
| offset | int | 0 | 0 | — | Number of items to skip |
| limit | int | 50 | 1 | 200 | Number of items to return |

## Error Response Format (RFC 9457 Problem Details)

All error responses use `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Table 'SalesLT.NonExistent' was not found in the semantic model.",
  "instance": "/api/tables/SalesLT/NonExistent"
}
```

### Error Scenarios

| Status | Condition | `type` suffix |
|--------|-----------|---------------|
| 400 | Empty or invalid schema/name, invalid pagination params | `#section-15.5.1` |
| 404 | Entity not found in semantic model | `#section-15.5.5` |
| 503 | Semantic model not loaded or loading failed | `#section-15.6.4` |

## CORS

- Development: Allow all origins (`*`)
- Production: Configured allow-list via `appsettings.json` → `Cors:AllowedOrigins`

## Authentication

None for initial version (deferred per spec assumptions).
