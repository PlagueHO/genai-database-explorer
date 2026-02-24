# Tasks: Frontend Semantic Model Explorer

**Input**: Design documents from `/specs/004-frontend-semantic-explorer/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not explicitly requested in spec. Tests omitted from task phases.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `genai-database-explorer-service/src/` (existing .NET project)
- **Frontend**: `genai-database-explorer-frontend/src/` (new React project)
- **Backend tests**: `genai-database-explorer-service/tests/unit/`
- **Frontend tests**: `genai-database-explorer-frontend/tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize the React frontend project and configure the development toolchain.

- [ ] T001 Scaffold Vite + React 19 + TypeScript project in `genai-database-explorer-frontend/` using `pnpm create vite`
- [ ] T002 Install core dependencies: `@fluentui/react-components`, `tailwindcss`, `@tanstack/react-query`, `react-router` in `genai-database-explorer-frontend/package.json`
- [ ] T003 [P] Configure Tailwind CSS v4 in `genai-database-explorer-frontend/tailwind.config.ts` and import in `genai-database-explorer-frontend/src/main.tsx`
- [ ] T004 [P] Configure ESLint + Prettier for frontend in `genai-database-explorer-frontend/eslint.config.js` and `genai-database-explorer-frontend/.prettierrc`
- [ ] T005 [P] Configure Vitest in `genai-database-explorer-frontend/vite.config.ts` and install `vitest`, `@testing-library/react`, `@testing-library/jest-dom` as dev dependencies
- [ ] T006 [P] Configure Playwright for E2E tests in `genai-database-explorer-frontend/playwright.config.ts`
- [ ] T007 Define all TypeScript API interfaces in `genai-database-explorer-frontend/src/types/api.ts` per data-model.md (ProjectInfo, SemanticModelSummary, PaginatedResponse, EntitySummary, TableDetail, ViewDetail, StoredProcedureDetail, Column, Index, UpdateEntityDescriptionRequest, UpdateColumnDescriptionRequest, ProblemDetails)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented.

**CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T008 Implement base API client with typed fetch wrapper, error handling, and base URL configuration via `VITE_API_BASE_URL` in `genai-database-explorer-frontend/src/api/client.ts`
- [ ] T009 [P] Implement project API client (`getProject`) in `genai-database-explorer-frontend/src/api/projectApi.ts`
- [ ] T010 [P] Implement model API client (`getModel`, `reloadModel`) in `genai-database-explorer-frontend/src/api/modelApi.ts`
- [ ] T011 [P] Implement tables API client (`listTables`, `getTable`, `patchTable`, `patchTableColumn`) in `genai-database-explorer-frontend/src/api/tablesApi.ts`
- [ ] T012 [P] Implement views API client (`listViews`, `getView`, `patchView`, `patchViewColumn`) in `genai-database-explorer-frontend/src/api/viewsApi.ts`
- [ ] T013 [P] Implement stored procedures API client (`listStoredProcedures`, `getStoredProcedure`, `patchStoredProcedure`) in `genai-database-explorer-frontend/src/api/storedProceduresApi.ts`
- [ ] T014 Create `AppUIContext` provider with `sidebarCollapsed` and `chatPanelOpen` state in `genai-database-explorer-frontend/src/context/AppUIContext.tsx`
- [ ] T015 Configure React Query `QueryClientProvider` and React Router in `genai-database-explorer-frontend/src/App.tsx` with FluentUI `FluentProvider` wrapping
- [ ] T016 Implement `AppLayout` component with left sidebar, main content area (Router Outlet), and collapsible right chat panel in `genai-database-explorer-frontend/src/components/layout/AppLayout.tsx`
- [ ] T017 [P] Implement `Sidebar` component with navigation links (Dashboard, Tables, Views, Stored Procedures) using FluentUI `NavDrawer`/`Nav` in `genai-database-explorer-frontend/src/components/layout/Sidebar.tsx`
- [ ] T018 [P] Implement `LoadingSpinner` component in `genai-database-explorer-frontend/src/components/common/LoadingSpinner.tsx` using FluentUI `Spinner`
- [ ] T019 [P] Implement `ErrorBanner` component in `genai-database-explorer-frontend/src/components/common/ErrorBanner.tsx` using FluentUI `MessageBar`
- [ ] T020 [P] Implement `EmptyState` component in `genai-database-explorer-frontend/src/components/common/EmptyState.tsx`
- [ ] T021 Define all routes in `genai-database-explorer-frontend/src/App.tsx`: `/` (Dashboard), `/tables`, `/tables/:schema/:name`, `/views`, `/views/:schema/:name`, `/stored-procedures`, `/stored-procedures/:schema/:name`
- [ ] T022 Add `Aspire.Hosting.JavaScript` NuGet package to `genai-database-explorer-service/src/GenAIDBExplorer.AppHost/GenAIDBExplorer.AppHost.csproj`
- [ ] T023 Wire frontend into Aspire AppHost using `AddViteApp()` with `WithReference(api).WaitFor(api)` in `genai-database-explorer-service/src/GenAIDBExplorer.AppHost/Program.cs`

**Checkpoint**: Foundation ready — layout renders with sidebar navigation and routing. API client layer is complete. Aspire orchestration starts both frontend and backend.

---

## Phase 3: User Story 1 — Browse Semantic Model Overview (Priority: P1) MVP

**Goal**: Display a dashboard with project info, model summary (name, source, description, entity counts, persistence strategy), and error handling when API is unreachable.

**Independent Test**: Open the app → dashboard shows model name, source, description, table/view/stored procedure counts, and persistence strategy. If API is down, an error message is displayed.

- [ ] T024 [P] [US1] Implement `useProject` React Query hook in `genai-database-explorer-frontend/src/hooks/useProject.ts`
- [ ] T025 [P] [US1] Implement `useModel` React Query hook (query + reload mutation) in `genai-database-explorer-frontend/src/hooks/useModel.ts`
- [ ] T026 [US1] Implement `DashboardPage` with `ProjectInfoCard` and `ModelSummaryCard` sections displaying project path, model name, source, description, persistence strategy, and entity counts in `genai-database-explorer-frontend/src/pages/DashboardPage.tsx`

**Checkpoint**: User Story 1 is fully functional. Dashboard displays model overview with loading and error states.

---

## Phase 4: User Story 2 — Browse and Search Tables (Priority: P1) MVP

**Goal**: Paginated table list with search, table detail view with columns, indexes, and NotUsed status display.

**Independent Test**: Navigate to Tables → see paginated list → search filters correctly → click table → detail view shows columns, indexes, NotUsed badge.

- [ ] T027 [P] [US2] Implement `SearchInput` component in `genai-database-explorer-frontend/src/components/common/SearchInput.tsx` using FluentUI `SearchBox`
- [ ] T028 [P] [US2] Implement `Pagination` component with offset/limit controls in `genai-database-explorer-frontend/src/components/common/Pagination.tsx`
- [ ] T029 [P] [US2] Implement `EntityList` generic component rendering `EntitySummary` items with schema, name, description, NotUsed badge in `genai-database-explorer-frontend/src/components/entities/EntityList.tsx`
- [ ] T030 [P] [US2] Implement `EntityHeader` component showing `Schema.Name` heading with NotUsed badge in `genai-database-explorer-frontend/src/components/entities/EntityHeader.tsx`
- [ ] T031 [P] [US2] Implement `ColumnsTable` read-only component displaying column name, type, description, isPrimaryKey, isNullable, etc. in `genai-database-explorer-frontend/src/components/entities/ColumnsTable.tsx`
- [ ] T032 [P] [US2] Implement `IndexesTable` read-only component displaying index name, type, columns, isUnique, isPrimaryKey in `genai-database-explorer-frontend/src/components/entities/IndexesTable.tsx`
- [ ] T033 [US2] Implement `useTables` React Query hooks (list query with pagination params, detail query by schema/name) in `genai-database-explorer-frontend/src/hooks/useTables.ts`
- [ ] T034 [US2] Implement `TablesListPage` with `SearchInput`, `EntityList`, and `Pagination` wired to `useTables` list hook with client-side search filtering in `genai-database-explorer-frontend/src/pages/TablesListPage.tsx`
- [ ] T035 [US2] Implement `TableDetailPage` with `EntityHeader`, description fields, `ColumnsTable`, and `IndexesTable` wired to `useTables` detail hook in `genai-database-explorer-frontend/src/pages/TableDetailPage.tsx`

**Checkpoint**: User Story 2 is fully functional. Tables list is browsable, searchable, paginated. Table detail displays all columns and indexes.

---

## Phase 5: User Story 3 — Browse and Search Views (Priority: P2)

**Goal**: Paginated view list with search, view detail with columns and SQL definition display.

**Independent Test**: Navigate to Views → see paginated list → search filters correctly → click view → detail shows columns and SQL definition.

- [ ] T036 [P] [US3] Implement `DefinitionViewer` component displaying SQL definition text with code formatting in `genai-database-explorer-frontend/src/components/entities/DefinitionViewer.tsx`
- [ ] T037 [US3] Implement `useViews` React Query hooks (list query with pagination, detail query by schema/name) in `genai-database-explorer-frontend/src/hooks/useViews.ts`
- [ ] T038 [US3] Implement `ViewsListPage` with `SearchInput`, `EntityList`, and `Pagination` wired to `useViews` list hook in `genai-database-explorer-frontend/src/pages/ViewsListPage.tsx`
- [ ] T039 [US3] Implement `ViewDetailPage` with `EntityHeader`, description fields, `ColumnsTable`, and `DefinitionViewer` wired to `useViews` detail hook in `genai-database-explorer-frontend/src/pages/ViewDetailPage.tsx`

**Checkpoint**: User Story 3 is fully functional. Views browsing, search, and detail with SQL definition work independently.

---

## Phase 6: User Story 4 — Browse and Search Stored Procedures (Priority: P2)

**Goal**: Paginated stored procedure list with search, stored procedure detail with parameters and SQL definition.

**Independent Test**: Navigate to Stored Procedures → paginated list → click one → detail shows parameters, definition, and metadata.

- [ ] T040 [P] [US4] Implement `ParametersDisplay` component showing stored procedure parameters text in `genai-database-explorer-frontend/src/components/entities/ParametersDisplay.tsx`
- [ ] T041 [US4] Implement `useStoredProcedures` React Query hooks (list query with pagination, detail query by schema/name) in `genai-database-explorer-frontend/src/hooks/useStoredProcedures.ts`
- [ ] T042 [US4] Implement `StoredProceduresListPage` with `SearchInput`, `EntityList`, and `Pagination` wired to `useStoredProcedures` list hook in `genai-database-explorer-frontend/src/pages/StoredProceduresListPage.tsx`
- [ ] T043 [US4] Implement `StoredProcedureDetailPage` with `EntityHeader`, description fields, `ParametersDisplay`, and `DefinitionViewer` wired to `useStoredProcedures` detail hook in `genai-database-explorer-frontend/src/pages/StoredProcedureDetailPage.tsx`

**Checkpoint**: User Story 4 is fully functional. All three entity types (tables, views, stored procedures) are browsable.

---

## Phase 7: User Story 5 — Edit Entity Descriptions (Priority: P2)

**Goal**: Inline editing of entity-level Description, Semantic Description, NotUsed, and NotUsedReason for tables/views/stored procedures. Column-level Description and Semantic Description editing for tables and views.

**Independent Test**: Open table detail → edit description → save → reload page → change persists. Edit column description → save → persists. Toggle NotUsed → save → persists.

### Backend API Extensions

- [ ] T044 [US5] Extend `UpdateEntityDescriptionRequest` record to add `bool? NotUsed` and `string? NotUsedReason` properties in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Models/UpdateEntityDescriptionRequest.cs`
- [ ] T045 [US5] Update PATCH validation and handler in `TableEndpoints.cs` to apply `NotUsed` and `NotUsedReason` fields when provided in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/TableEndpoints.cs`
- [ ] T046 [P] [US5] Update PATCH validation and handler in `ViewEndpoints.cs` to apply `NotUsed` and `NotUsedReason` fields when provided in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/ViewEndpoints.cs`
- [ ] T047 [P] [US5] Update PATCH validation and handler in `StoredProcedureEndpoints.cs` to apply `NotUsed` and `NotUsedReason` fields when provided in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/StoredProcedureEndpoints.cs`
- [ ] T048 [US5] Create `UpdateColumnDescriptionRequest` record with `string? Description` and `string? SemanticDescription` in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Models/UpdateColumnDescriptionRequest.cs`
- [ ] T049 [US5] Add `PATCH /{schema}/{name}/columns/{columnName}` endpoint to `TableEndpoints.cs` that finds a column by name and updates its Description/SemanticDescription in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/TableEndpoints.cs`
- [ ] T050 [US5] Add `PATCH /{schema}/{name}/columns/{columnName}` endpoint to `ViewEndpoints.cs` that finds a column by name and updates its Description/SemanticDescription in `genai-database-explorer-service/src/GenAIDBExplorer.Api/Endpoints/ViewEndpoints.cs`
- [ ] T051 [US5] Run `dotnet format` on changed backend files and verify `dotnet build` succeeds for `genai-database-explorer-service/GenAIDBExplorer.slnx`

### Frontend Editing Components

- [ ] T052 [P] [US5] Implement `EditableField` component with view/edit modes, save/cancel buttons, multiline support using FluentUI `Textarea`/`Input` in `genai-database-explorer-frontend/src/components/common/EditableField.tsx`
- [ ] T053 [P] [US5] Implement `NotUsedEditor` component with FluentUI `Switch` for NotUsed flag and conditional `Input` for NotUsedReason in `genai-database-explorer-frontend/src/components/common/NotUsedEditor.tsx`
- [ ] T054 [US5] Add mutation hooks to `useTables` for entity PATCH (`patchTable`) and column PATCH (`patchTableColumn`) with query invalidation in `genai-database-explorer-frontend/src/hooks/useTables.ts`
- [ ] T055 [P] [US5] Add mutation hooks to `useViews` for entity PATCH (`patchView`) and column PATCH (`patchViewColumn`) with query invalidation in `genai-database-explorer-frontend/src/hooks/useViews.ts`
- [ ] T056 [P] [US5] Add mutation hook to `useStoredProcedures` for entity PATCH (`patchStoredProcedure`) with query invalidation in `genai-database-explorer-frontend/src/hooks/useStoredProcedures.ts`
- [ ] T057 [US5] Update `TableDetailPage` to use `EditableField` for description/semanticDescription, `NotUsedEditor` for NotUsed, and `EditableColumnRow` in `ColumnsTable` for column-level editing in `genai-database-explorer-frontend/src/pages/TableDetailPage.tsx`
- [ ] T058 [US5] Update `ViewDetailPage` to use `EditableField`, `NotUsedEditor`, and column-level editing in `genai-database-explorer-frontend/src/pages/ViewDetailPage.tsx`
- [ ] T059 [US5] Update `StoredProcedureDetailPage` to use `EditableField` and `NotUsedEditor` in `genai-database-explorer-frontend/src/pages/StoredProcedureDetailPage.tsx`
- [ ] T060 [US5] Add error handling and success feedback (FluentUI `Toast`) for all PATCH mutations across detail pages

**Checkpoint**: User Story 5 is fully functional. Entity and column descriptions are editable, NotUsed flag is toggleable, changes persist via API.

---

## Phase 8: User Story 6 — Chat Interface Placeholder (Priority: P3)

**Goal**: Collapsible right chat panel with message display area, input area, and "coming soon" message. Layout follows AG-UI conversational pattern for future CopilotKit integration.

**Independent Test**: Open app → toggle chat panel → panel slides in → type message → "coming soon" response appears.

- [ ] T061 [US6] Implement `ChatPanel` component with collapsible panel, message display area, message input (disabled), and "Agentic chat capabilities coming soon" placeholder in `genai-database-explorer-frontend/src/components/layout/ChatPanel.tsx`
- [ ] T062 [US6] Add chat panel toggle button to `AppLayout` header/toolbar area and wire to `AppUIContext.chatPanelOpen` state in `genai-database-explorer-frontend/src/components/layout/AppLayout.tsx`

**Checkpoint**: User Story 6 is fully functional. Chat panel is visible, collapsible, and shows placeholder message.

---

## Phase 9: User Story 7 — Reload Semantic Model (Priority: P3)

**Goal**: Reload button in sidebar triggers `POST /api/model/reload`, invalidates all cached data, and shows loading indicator.

**Independent Test**: Click reload → loading indicator appears → model refreshes → dashboard updates with new counts.

- [ ] T063 [US7] Add `ModelReloadButton` to `Sidebar` component that triggers `useModel` reload mutation, invalidates all React Query caches (`queryClient.invalidateQueries()`), and shows loading indicator in `genai-database-explorer-frontend/src/components/layout/Sidebar.tsx`

**Checkpoint**: User Story 7 is fully functional. Model can be reloaded from persistence store without restarting the application.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories.

- [ ] T064 [P] Add handling for edge cases: empty entity lists (empty state messages per FR-014), tables with no columns, not-found entities after reload (404 → redirect to list) across all detail pages
- [ ] T065 [P] Add handling for long text overflow in description/semanticDescription fields (scrolling or truncation with expand) across all detail and list components
- [ ] T066 [P] Add responsive layout handling for sidebar collapse on small viewports in `genai-database-explorer-frontend/src/components/layout/AppLayout.tsx`
- [ ] T067 Validate `aspire run` starts both frontend and backend, service discovery injects API URL correctly, and end-to-end browsing and editing workflow completes successfully
- [ ] T068 Run ESLint and Prettier on all frontend files, fix any issues
- [ ] T069 Run `dotnet format` on all modified backend C# files via `format-fix-whitespace-only` task
- [ ] T070 Verify `pnpm build` produces a production build without errors in `genai-database-explorer-frontend/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on T001, T002, T007 from Setup — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational completion (T008–T023)
- **US2 (Phase 4)**: Depends on Foundational completion — can run in parallel with US1
- **US3 (Phase 5)**: Depends on Foundational completion — can run in parallel with US1, US2
- **US4 (Phase 6)**: Depends on Foundational completion — can run in parallel with US1–US3
- **US5 (Phase 7)**: Depends on US2, US3, US4 detail pages existing (T035, T039, T043) — backend tasks (T044–T051) can start after Foundational
- **US6 (Phase 8)**: Depends on Foundational `AppLayout` (T016) — can run in parallel with US1–US5
- **US7 (Phase 9)**: Depends on US1 `useModel` hook (T025) and Sidebar (T017) — can run in parallel with US2–US6
- **Polish (Phase 10)**: Depends on all user stories being complete

### User Story Dependencies

- **US1** (P1): Independent — only requires Foundational
- **US2** (P1): Independent — only requires Foundational; introduces shared components (SearchInput, Pagination, EntityList, EntityHeader, ColumnsTable, IndexesTable) reused by US3, US4
- **US3** (P2): Reuses components from US2 — can start after Foundational but benefits from US2 completing first
- **US4** (P2): Reuses components from US2 — can start after Foundational but benefits from US2 completing first
- **US5** (P2): Requires detail pages from US2, US3, US4 to exist for editing integration; backend changes (T044–T051) can start independently
- **US6** (P3): Independent — only requires Foundational layout
- **US7** (P3): Independent — only requires US1 `useModel` hook

### Within Each User Story

- API client functions (Phase 2) before hooks
- Hooks before pages
- Shared components before pages that use them
- Backend API extensions (US5) before frontend mutation hooks

### Parallel Opportunities

- T003, T004, T005, T006 can all run in parallel (Setup config files)
- T009, T010, T011, T012, T013 can all run in parallel (API client modules after T008)
- T017, T018, T019, T020 can all run in parallel (independent components)
- T027, T028, T029, T030, T031, T032 can all run in parallel (US2 shared components)
- T046, T047 can run in parallel after T044, T045 (backend PATCH updates for views/stored procs)
- T052, T053 can run in parallel (editing components)
- T054, T055, T056 can run in parallel (mutation hooks after backend ready)
- US6 (T061–T062) can proceed independently alongside any other user story

---

## Parallel Example: User Story 2

```text
# Launch all shared components in parallel:
Task T027: SearchInput component
Task T028: Pagination component
Task T029: EntityList component
Task T030: EntityHeader component
Task T031: ColumnsTable component
Task T032: IndexesTable component

# Then sequentially:
Task T033: useTables hooks (depends on T011 API client)
Task T034: TablesListPage (depends on T027, T028, T029, T033)
Task T035: TableDetailPage (depends on T030, T031, T032, T033)
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup
1. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
1. Complete Phase 3: User Story 1 (Dashboard)
1. Complete Phase 4: User Story 2 (Tables browsing)
1. **STOP and VALIDATE**: Test US1 + US2 independently — user can browse model overview and tables

### Incremental Delivery

1. Setup + Foundational → Foundation ready
1. US1 (Dashboard) → Test → First visible output
1. US2 (Tables) → Test → Core browsing works (MVP!)
1. US3 (Views) + US4 (Stored Procedures) → Test → Full browsing
1. US5 (Editing) → Test → Full editing capability
1. US6 (Chat placeholder) + US7 (Reload) → Test → Feature complete
1. Polish → Validate → Done

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Backend API changes (T044–T051) require `dotnet format` after edits per constitution
- Frontend project uses `pnpm` as package manager (matches repo root)
- Total tasks: 70
