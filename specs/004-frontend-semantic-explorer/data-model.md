# Data Model: Frontend Semantic Model Explorer

**Feature**: `004-frontend-semantic-explorer`
**Date**: 2026-02-24

## Overview

This document defines the TypeScript data model for the frontend application. All types mirror the backend API DTOs (C# records in `GenAIDBExplorer.Api.Models`) to ensure type-safe API communication.

## API Response Types

These types map 1:1 to the backend C# response records.

### Core Responses

```typescript
/** Maps to: ProjectInfoResponse.cs */
interface ProjectInfo {
  projectPath: string;
  modelName: string;
  modelSource: string;
  persistenceStrategy: string;
  modelLoaded: boolean;
}

/** Maps to: SemanticModelSummaryResponse.cs */
interface SemanticModelSummary {
  name: string;
  source: string;
  description: string | null;
  tableCount: number;
  viewCount: number;
  storedProcedureCount: number;
}

/** Maps to: PaginatedResponse<T>.cs */
interface PaginatedResponse<T> {
  items: readonly T[];
  totalCount: number;
  offset: number;
  limit: number;
}
```

### Entity Responses

```typescript
/** Maps to: EntitySummaryResponse.cs — used in paginated list endpoints */
interface EntitySummary {
  schema: string;
  name: string;
  description: string | null;
  semanticDescription: string | null;
  notUsed: boolean;
}

/** Maps to: TableDetailResponse.cs */
interface TableDetail {
  schema: string;
  name: string;
  description: string | null;
  semanticDescription: string | null;
  semanticDescriptionLastUpdate: string | null; // ISO 8601 datetime
  details: string | null;
  additionalInformation: string | null;
  notUsed: boolean;
  notUsedReason: string | null;
  columns: readonly Column[];
  indexes: readonly Index[];
}

/** Maps to: ViewDetailResponse.cs */
interface ViewDetail {
  schema: string;
  name: string;
  description: string | null;
  semanticDescription: string | null;
  semanticDescriptionLastUpdate: string | null; // ISO 8601 datetime
  additionalInformation: string | null;
  definition: string;
  notUsed: boolean;
  notUsedReason: string | null;
  columns: readonly Column[];
}

/** Maps to: StoredProcedureDetailResponse.cs */
interface StoredProcedureDetail {
  schema: string;
  name: string;
  description: string | null;
  semanticDescription: string | null;
  semanticDescriptionLastUpdate: string | null; // ISO 8601 datetime
  additionalInformation: string | null;
  parameters: string | null; // Plain text, not typed list
  definition: string;
  notUsed: boolean;
  notUsedReason: string | null;
}
```

### Sub-Entity Responses

```typescript
/** Maps to: ColumnResponse.cs */
interface Column {
  name: string;
  type: string | null;
  description: string | null;
  isPrimaryKey: boolean;
  isNullable: boolean;
  isIdentity: boolean;
  isComputed: boolean;
  isXmlDocument: boolean;
  maxLength: number | null;
  precision: number | null;
  scale: number | null;
  referencedTable: string | null;
  referencedColumn: string | null;
}

/** Maps to: IndexResponse.cs */
interface Index {
  name: string;
  type: string | null;
  columnName: string | null;
  isUnique: boolean;
  isPrimaryKey: boolean;
  isUniqueConstraint: boolean;
}
```

## API Request Types

### Existing (to be extended)

```typescript
/**
 * Maps to: UpdateEntityDescriptionRequest.cs
 * BACKEND CHANGE REQUIRED: Add NotUsed and NotUsedReason fields
 * Current backend only has Description and SemanticDescription.
 */
interface UpdateEntityDescriptionRequest {
  description?: string | null;
  semanticDescription?: string | null;
  notUsed?: boolean | null;       // NEW — requires backend extension
  notUsedReason?: string | null;  // NEW — requires backend extension
}
```

### New (column-level editing)

```typescript
/**
 * NEW endpoint request — no backend equivalent exists yet.
 * For PATCH /api/tables/{schema}/{name}/columns/{columnName}
 * and PATCH /api/views/{schema}/{name}/columns/{columnName}
 */
interface UpdateColumnDescriptionRequest {
  description?: string | null;
  semanticDescription?: string | null;
}
```

## Error Responses

The backend uses RFC 9457 Problem Details format:

```typescript
/** Standard RFC 9457 Problem Details response from ASP.NET Core */
interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance?: string;
}
```

## Component Hierarchy

```text
App
├── AppLayout
│   ├── Sidebar (left)
│   │   ├── SidebarNav
│   │   │   ├── NavLink → / (Dashboard)
│   │   │   ├── NavLink → /tables
│   │   │   ├── NavLink → /views
│   │   │   └── NavLink → /stored-procedures
│   │   └── ModelReloadButton
│   ├── MainContent (center)
│   │   └── <Router Outlet>
│   │       ├── DashboardPage (/)
│   │       │   ├── ProjectInfoCard
│   │       │   └── ModelSummaryCard
│   │       ├── TablesListPage (/tables)
│   │       │   ├── SearchInput
│   │       │   ├── EntityList<EntitySummary>
│   │       │   └── Pagination
│   │       ├── TableDetailPage (/tables/:schema/:name)
│   │       │   ├── EntityHeader (schema, name, NotUsed badge)
│   │       │   ├── EditableField (description)
│   │       │   ├── EditableField (semanticDescription)
│   │       │   ├── NotUsedEditor (notUsed flag + reason)
│   │       │   ├── ColumnsTable
│   │       │   │   └── EditableColumnRow (per column)
│   │       │   └── IndexesTable
│   │       ├── ViewsListPage (/views)
│   │       │   ├── SearchInput
│   │       │   ├── EntityList<EntitySummary>
│   │       │   └── Pagination
│   │       ├── ViewDetailPage (/views/:schema/:name)
│   │       │   ├── EntityHeader
│   │       │   ├── EditableField (description)
│   │       │   ├── EditableField (semanticDescription)
│   │       │   ├── NotUsedEditor
│   │       │   ├── ColumnsTable
│   │       │   │   └── EditableColumnRow (per column)
│   │       │   └── DefinitionViewer
│   │       ├── StoredProceduresListPage (/stored-procedures)
│   │       │   ├── SearchInput
│   │       │   ├── EntityList<EntitySummary>
│   │       │   └── Pagination
│   │       └── StoredProcedureDetailPage (/stored-procedures/:schema/:name)
│   │           ├── EntityHeader
│   │           ├── EditableField (description)
│   │           ├── EditableField (semanticDescription)
│   │           ├── NotUsedEditor
│   │           ├── ParametersDisplay
│   │           └── DefinitionViewer
│   └── ChatPanel (right, collapsible)
│       ├── ChatPanelToggle
│       ├── MessageArea (placeholder)
│       └── MessageInput (disabled, "Coming soon" text)
```

## Shared Components

| Component | Purpose | Props |
|-----------|---------|-------|
| `SearchInput` | Text search filter for entity lists | `value`, `onChange`, `placeholder` |
| `Pagination` | Offset/limit pagination controls | `totalCount`, `offset`, `limit`, `onChange` |
| `EntityList` | Generic list of `EntitySummary` items | `items`, `onSelect`, `entityType` |
| `EntityHeader` | Schema.Name heading with NotUsed badge | `schema`, `name`, `notUsed` |
| `EditableField` | Inline edit with save/cancel for text | `label`, `value`, `onSave`, `multiline` |
| `NotUsedEditor` | Toggle + reason text field | `notUsed`, `notUsedReason`, `onSave` |
| `ColumnsTable` | Table of columns with inline editing | `columns`, `onSaveColumn`, `readOnlyFields` |
| `EditableColumnRow` | Single column row with editable desc fields | `column`, `onSave` |
| `IndexesTable` | Read-only table of indexes | `indexes` |
| `DefinitionViewer` | SQL definition display with syntax highlighting | `definition` |
| `ParametersDisplay` | Displays stored procedure parameters text | `parameters` |
| `ErrorBanner` | Displays error messages | `error`, `onDismiss` |
| `LoadingSpinner` | Loading indicator | `label` |
| `EmptyState` | "No items" message | `message`, `icon` |

## Data Flow

### Read Operations (lists and details)

```text
Component Mount
  → React Query hook (useQuery)
    → API client function (fetch)
      → GET /api/{entity-type}/...
        → JSON response
    → Cache result (staleTime: 5 min)
  → Render data
```

### Write Operations (entity and column edits)

```text
User edits field → local state update
  → User clicks Save
    → React Query mutation (useMutation)
      → API client function (fetch)
        → PATCH /api/{entity-type}/{schema}/{name}
          → Updated entity JSON response
      → Invalidate related queries (list + detail)
    → Update UI with response data
    → Show success/error toast
```

### Model Reload

```text
User clicks Reload
  → React Query mutation
    → POST /api/model/reload
      → SemanticModelSummaryResponse
    → Invalidate ALL queries (queryClient.invalidateQueries())
  → Redirect to dashboard
```

## State Management Structure

### Server State (React Query)

| Query Key | Endpoint | Stale Time |
|-----------|----------|------------|
| `['project']` | `GET /api/project` | 10 min |
| `['model']` | `GET /api/model` | 5 min |
| `['tables', { offset, limit }]` | `GET /api/tables?offset=&limit=` | 5 min |
| `['tables', schema, name]` | `GET /api/tables/{schema}/{name}` | 5 min |
| `['views', { offset, limit }]` | `GET /api/views?offset=&limit=` | 5 min |
| `['views', schema, name]` | `GET /api/views/{schema}/{name}` | 5 min |
| `['storedProcedures', { offset, limit }]` | `GET /api/stored-procedures?offset=&limit=` | 5 min |
| `['storedProcedures', schema, name]` | `GET /api/stored-procedures/{schema}/{name}` | 5 min |

### UI State (React Context)

```typescript
interface AppUIState {
  sidebarCollapsed: boolean;
  chatPanelOpen: boolean;
}
```

## Backend Changes Required

### 1. Extend `UpdateEntityDescriptionRequest`

Add `NotUsed` (bool?) and `NotUsedReason` (string?) to support FR-006.

### 2. Update PATCH handlers

All three entity PATCH handlers (`TableEndpoints`, `ViewEndpoints`, `StoredProcedureEndpoints`) must apply `NotUsed` and `NotUsedReason` when provided.

### 3. Add column-level PATCH endpoints

- `PATCH /api/tables/{schema}/{name}/columns/{columnName}` — update column `Description` and `SemanticDescription`
- `PATCH /api/views/{schema}/{name}/columns/{columnName}` — update column `Description` and `SemanticDescription`

New request type: `UpdateColumnDescriptionRequest(string? Description, string? SemanticDescription)`

### 4. Aspire AppHost integration

Add `Aspire.Hosting.JavaScript` v13.1.0 NuGet package to AppHost and wire up the frontend with `AddViteApp()`.
