# API Contracts: Frontend Semantic Model Explorer

**Feature**: `004-frontend-semantic-explorer`
**Date**: 2026-02-24

This document defines the complete API contract between the frontend and backend. It covers existing endpoints (already implemented) and new endpoints (required for FR-006 and FR-007).

---

## Existing Endpoints (No Changes Required)

### GET /api/project

Returns project configuration information.

**Response** `200 OK`:

```json
{
  "projectPath": "string",
  "modelName": "string",
  "modelSource": "string",
  "persistenceStrategy": "string",
  "modelLoaded": true
}
```

### GET /api/model

Returns semantic model summary.

**Response** `200 OK`:

```json
{
  "name": "string",
  "source": "string",
  "description": "string | null",
  "tableCount": 0,
  "viewCount": 0,
  "storedProcedureCount": 0
}
```

### POST /api/model/reload

Reloads the semantic model from persistence store.

**Response** `200 OK`: Returns `SemanticModelSummaryResponse` (same as GET /api/model).

### GET /api/tables?offset={offset}&limit={limit}

Lists tables with pagination. Default offset=0, limit=50, max limit=200.

**Response** `200 OK`:

```json
{
  "items": [
    {
      "schema": "string",
      "name": "string",
      "description": "string | null",
      "semanticDescription": "string | null",
      "notUsed": false
    }
  ],
  "totalCount": 0,
  "offset": 0,
  "limit": 50
}
```

### GET /api/tables/{schema}/{name}

Returns table detail including columns and indexes.

**Response** `200 OK`:

```json
{
  "schema": "string",
  "name": "string",
  "description": "string | null",
  "semanticDescription": "string | null",
  "semanticDescriptionLastUpdate": "string | null",
  "details": "string | null",
  "additionalInformation": "string | null",
  "notUsed": false,
  "notUsedReason": "string | null",
  "columns": [
    {
      "name": "string",
      "type": "string | null",
      "description": "string | null",
      "isPrimaryKey": false,
      "isNullable": true,
      "isIdentity": false,
      "isComputed": false,
      "isXmlDocument": false,
      "maxLength": null,
      "precision": null,
      "scale": null,
      "referencedTable": "string | null",
      "referencedColumn": "string | null"
    }
  ],
  "indexes": [
    {
      "name": "string",
      "type": "string | null",
      "columnName": "string | null",
      "isUnique": false,
      "isPrimaryKey": false,
      "isUniqueConstraint": false
    }
  ]
}
```

**Response** `404 Not Found`: Problem Details

### GET /api/views?offset={offset}&limit={limit}

Lists views with pagination. Same pagination semantics as tables.

**Response** `200 OK`: `PaginatedResponse<EntitySummaryResponse>` (same shape as tables list).

### GET /api/views/{schema}/{name}

Returns view detail including columns and SQL definition.

**Response** `200 OK`:

```json
{
  "schema": "string",
  "name": "string",
  "description": "string | null",
  "semanticDescription": "string | null",
  "semanticDescriptionLastUpdate": "string | null",
  "additionalInformation": "string | null",
  "definition": "string",
  "notUsed": false,
  "notUsedReason": "string | null",
  "columns": [{ "...same as table columns..." }]
}
```

**Response** `404 Not Found`: Problem Details

### GET /api/stored-procedures?offset={offset}&limit={limit}

Lists stored procedures with pagination. Same pagination semantics as tables.

**Response** `200 OK`: `PaginatedResponse<EntitySummaryResponse>` (same shape as tables list).

### GET /api/stored-procedures/{schema}/{name}

Returns stored procedure detail.

**Response** `200 OK`:

```json
{
  "schema": "string",
  "name": "string",
  "description": "string | null",
  "semanticDescription": "string | null",
  "semanticDescriptionLastUpdate": "string | null",
  "additionalInformation": "string | null",
  "parameters": "string | null",
  "definition": "string",
  "notUsed": false,
  "notUsedReason": "string | null"
}
```

**Response** `404 Not Found`: Problem Details

---

## Modified Endpoints (Backend Changes Required)

### PATCH /api/tables/{schema}/{name}

**Change**: Extend request body to include `notUsed` and `notUsedReason` fields (FR-006).

**Request Body** (extended):

```json
{
  "description": "string | null",
  "semanticDescription": "string | null",
  "notUsed": true,
  "notUsedReason": "string | null"
}
```

All fields are optional. At least one field must be provided (current validation: at least `description` or `semanticDescription`; extend to include `notUsed` or `notUsedReason`).

**Response** `200 OK`: `TableDetailResponse` (full table detail, same as GET).
**Response** `400 Bad Request`: Problem Details (no fields provided).
**Response** `404 Not Found`: Problem Details.

**Backend changes**:

1. Add `bool? NotUsed` and `string? NotUsedReason` to `UpdateEntityDescriptionRequest.cs`
1. Update validation: at least one of the four fields must be non-null
1. Apply `NotUsed` and `NotUsedReason` when provided in `PatchTable` handler

### PATCH /api/views/{schema}/{name}

Same changes as PATCH tables. Extended request body with `notUsed` and `notUsedReason`.

**Response** `200 OK`: `ViewDetailResponse`.

### PATCH /api/stored-procedures/{schema}/{name}

Same changes as PATCH tables. Extended request body with `notUsed` and `notUsedReason`.

**Response** `200 OK`: `StoredProcedureDetailResponse`.

---

## New Endpoints (Backend Addition Required)

### PATCH /api/tables/{schema}/{name}/columns/{columnName}

Update a table column's description fields (FR-007).

**Request Body**:

```json
{
  "description": "string | null",
  "semanticDescription": "string | null"
}
```

At least one field must be provided.

**Response** `200 OK`:

```json
{
  "name": "string",
  "type": "string | null",
  "description": "string | null",
  "isPrimaryKey": false,
  "isNullable": true,
  "isIdentity": false,
  "isComputed": false,
  "isXmlDocument": false,
  "maxLength": null,
  "precision": null,
  "scale": null,
  "referencedTable": "string | null",
  "referencedColumn": "string | null"
}
```

**Response** `400 Bad Request`: Problem Details (no fields provided).
**Response** `404 Not Found`: Problem Details (table or column not found).
**Response** `503 Service Unavailable`: Problem Details.

### PATCH /api/views/{schema}/{name}/columns/{columnName}

Update a view column's description fields (FR-007). Same request/response contract as table column PATCH.

---

## Error Response Format (All Endpoints)

All error responses use RFC 9457 Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Table 'SalesLT.Product' was not found in the semantic model."
}
```

## Common Error Codes

| Status | Meaning | When |
|--------|---------|------|
| `400` | Bad Request | No fields provided in PATCH request |
| `404` | Not Found | Entity or column not found in model |
| `503` | Service Unavailable | Model not loaded or backend error |
