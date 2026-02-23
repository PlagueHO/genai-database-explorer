# Data Model: REST API for Semantic Model Repository

**Feature**: 003-api-semantic-model
**Date**: 2026-02-23

## API Response Models (DTOs)

These are the response shapes returned by the API. They map from the existing `GenAIDBExplorer.Core.Models.SemanticModel` domain objects.

### SemanticModelSummaryResponse

Returned by `GET /api/model`.

| Field | Type | Description |
|-------|------|-------------|
| name | string | Name of the semantic model |
| source | string | Database source identifier |
| description | string? | Optional description |
| tableCount | int | Number of tables in the model |
| viewCount | int | Number of views in the model |
| storedProcedureCount | int | Number of stored procedures |

### PaginatedResponse\<T\>

Generic wrapper for all list endpoints.

| Field | Type | Description |
|-------|------|-------------|
| items | T[] | Page of results |
| totalCount | int | Total number of items across all pages |
| offset | int | Current offset |
| limit | int | Page size used |

### EntitySummaryResponse

Returned within paginated lists of tables, views, stored procedures.

| Field | Type | Description |
|-------|------|-------------|
| schema | string | Schema name (e.g., "SalesLT") |
| name | string | Entity name (e.g., "Product") |
| description | string? | User-provided description |
| semanticDescription | string? | AI-generated semantic description |
| notUsed | bool | Whether entity is marked as not used |

### TableDetailResponse

Returned by `GET /api/tables/{schema}/{name}`.

| Field | Type | Description |
|-------|------|-------------|
| schema | string | Schema name |
| name | string | Table name |
| description | string? | User-provided description |
| semanticDescription | string? | AI-generated description |
| semanticDescriptionLastUpdate | DateTime? | When semantic description was last updated |
| details | string? | Purpose details from data dictionary |
| additionalInformation | string? | Business rules or extra context |
| notUsed | bool | Whether table is marked not used |
| notUsedReason | string? | Why table is marked not used |
| columns | ColumnResponse[] | List of columns |
| indexes | IndexResponse[] | List of indexes |

### ViewDetailResponse

Returned by `GET /api/views/{schema}/{name}`.

| Field | Type | Description |
|-------|------|-------------|
| schema | string | Schema name |
| name | string | View name |
| description | string? | User-provided description |
| semanticDescription | string? | AI-generated description |
| semanticDescriptionLastUpdate | DateTime? | When semantic description was last updated |
| additionalInformation | string? | Business rules or extra context |
| definition | string | SQL definition of the view |
| notUsed | bool | Whether view is marked not used |
| notUsedReason | string? | Why view is marked not used |
| columns | ColumnResponse[] | List of columns |

### StoredProcedureDetailResponse

Returned by `GET /api/stored-procedures/{schema}/{name}`.

| Field | Type | Description |
|-------|------|-------------|
| schema | string | Schema name |
| name | string | Stored procedure name |
| description | string? | User-provided description |
| semanticDescription | string? | AI-generated description |
| semanticDescriptionLastUpdate | DateTime? | When semantic description was last updated |
| additionalInformation | string? | Business rules or extra context |
| parameters | string? | Parameter definitions |
| definition | string | SQL definition |
| notUsed | bool | Whether procedure is marked not used |
| notUsedReason | string? | Why procedure is marked not used |

### ColumnResponse

Nested within table and view detail responses.

| Field | Type | Description |
|-------|------|-------------|
| name | string | Column name |
| type | string? | Data type |
| description | string? | Column description |
| isPrimaryKey | bool | Primary key flag |
| isNullable | bool | Nullable flag |
| isIdentity | bool | Identity flag |
| isComputed | bool | Computed column flag |
| isXmlDocument | bool | XML document flag |
| maxLength | int? | Maximum length |
| precision | int? | Numeric precision |
| scale | int? | Numeric scale |
| referencedTable | string? | Foreign key target table |
| referencedColumn | string? | Foreign key target column |

### IndexResponse

Nested within table detail responses.

| Field | Type | Description |
|-------|------|-------------|
| name | string | Index name |
| type | string? | Index type (clustered, nonclustered) |
| columnName | string? | Column the index is on |
| isUnique | bool | Unique flag |
| isPrimaryKey | bool | Primary key flag |
| isUniqueConstraint | bool | Unique constraint flag |

### UpdateEntityDescriptionRequest

Request body for `PATCH /api/tables/{schema}/{name}`, etc.

| Field | Type | Description |
|-------|------|-------------|
| description | string? | New description (null = no change) |
| semanticDescription | string? | New semantic description (null = no change) |

### ProjectInfoResponse

Returned by `GET /api/project`.

| Field | Type | Description |
|-------|------|-------------|
| projectPath | string | Configured project directory |
| modelName | string | Name of the loaded semantic model |
| modelSource | string | Database source identifier |
| persistenceStrategy | string | Active persistence strategy name |
| modelLoaded | bool | Whether model is currently loaded |

## Entity Relationships

```text
SemanticModel (1)
  ├── Tables (0..*)
  │     ├── Columns (0..*)
  │     └── Indexes (0..*)
  ├── Views (0..*)
  │     └── Columns (0..*)
  └── StoredProcedures (0..*)
```

## State Transitions

The semantic model cache has three states:

```text
[Unloaded] --startup load--> [Loaded/Healthy]
[Loaded/Healthy] --reload initiated--> [Reloading]
[Reloading] --reload success--> [Loaded/Healthy]
[Reloading] --reload failure--> [Loaded/Healthy] (keeps previous model)
[Unloaded] --load failure--> [Error/Unhealthy]
```
