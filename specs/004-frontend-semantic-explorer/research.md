# Research: Frontend Semantic Model Explorer

**Feature**: `004-frontend-semantic-explorer`
**Date**: 2026-02-24

## 1. Aspire JavaScript/Vite Integration

**Decision**: Use `Aspire.Hosting.JavaScript` v13.1.0 with `AddViteApp()` to integrate the React frontend into the Aspire AppHost.

**Rationale**: Aspire 13.1 provides first-class `AddViteApp()` support via the `Aspire.Hosting.JavaScript` NuGet package. This is purpose-built for Vite-based SPAs (React+Vite is the standard toolchain). It handles:

- Starting the Vite dev server as a managed resource
- Service discovery (API URL injection via environment variables)
- Lifecycle management (start/stop with `aspire run`)
- Automatic Dockerfile generation for publishing

**Alternatives considered**:

- `AddJavaScriptApp()` — more generic, but `AddViteApp()` is specifically optimized for Vite projects (extends `JavaScriptAppResource` with Vite-specific behavior)
- `AddNpmApp()` — the older API, `AddViteApp()` is the recommended replacement for Vite-based apps
- Standalone (no Aspire) — rejected per clarification Q1; user explicitly chose Aspire integration

**AppHost integration pattern**:

```csharp
var api = builder.AddProject<Projects.GenAIDBExplorer_Api>("genaidbexplorer-api");

var frontend = builder.AddViteApp("genaidbexplorer-frontend", "../../../genai-database-explorer-frontend")
    .WithReference(api)
    .WaitFor(api);
```

**Package required**: `Aspire.Hosting.JavaScript` v13.1.0+ added to AppHost project.

## 2. Frontend Technology Stack

**Decision**: React 19 + Vite + TypeScript + Tailwind CSS v4 + FluentUI React Components v9

**Rationale**:

- **React 19** — latest stable, user-specified
- **Vite** — standard React build tool, required by Aspire `AddViteApp()`
- **TypeScript** — type safety for API integration, standard for FluentUI
- **Tailwind CSS v4** — latest version, user-specified for layout/utility styling
- **FluentUI React Components v9** (`@fluentui/react-components`) — user-specified, Microsoft design system providing accessible components
- **CopilotKit** — mentioned as future chat integration, explicitly out of scope for this phase

**Alternatives considered**:

- **Create React App** — deprecated by React team (February 2025), Vite is the recommended replacement
- **Next.js** — SSR framework, unnecessary complexity for a local development SPA tool
- **Material UI** — user explicitly chose FluentUI
- **CSS Modules/Styled Components** — user explicitly chose Tailwind CSS

## 3. Project Location and Structure

**Decision**: Place the frontend at `genai-database-explorer-frontend/` at the repository root, alongside `genai-database-explorer-service/`.

**Rationale**: The service code lives in `genai-database-explorer-service/`. A parallel top-level directory keeps the frontend clearly separated while maintaining the repository's naming convention. This is preferable to nesting inside the service folder since the frontend is a distinct technology stack (Node.js vs .NET).

**Alternatives considered**:

- Inside `genai-database-explorer-service/src/` — mixes Node.js and .NET projects, complicates build pipelines
- `frontend/` at root — too generic, doesn't match existing `genai-database-explorer-*` naming pattern
- `web/` at root — same problem as above

## 4. API Client Strategy

**Decision**: Use a typed API client service layer built on the native `fetch` API with TypeScript interfaces matching the backend DTO records.

**Rationale**: The backend API is a simple REST API with well-defined DTOs (records). A thin typed client layer provides type safety without pulling in heavy HTTP client libraries. React Query (TanStack Query) will handle caching, loading states, and error management.

**Alternatives considered**:

- **Axios** — adds a dependency for features `fetch` already provides, unnecessary for a simple REST API
- **OpenAPI code generation** — the API is small enough (12 endpoints) that hand-written types are simpler and more maintainable
- **SWR** — similar to React Query but less feature-rich for mutations (PATCH operations)

## 5. State Management

**Decision**: Use React Query (TanStack Query v5) for server state and React context for minimal UI state (sidebar collapsed, chat panel open).

**Rationale**: The application is primarily a data viewer/editor. Server state (model, entities, details) is the dominant state type. React Query handles caching, deduplication, background refresh, and optimistic updates for mutations. Local UI state is minimal (sidebar toggle, chat panel toggle) and doesn't warrant a state management library.

**Alternatives considered**:

- **Redux** — heavyweight for this use case; the app has no complex client-side state
- **Zustand** — simpler than Redux but still unnecessary when React Query + Context suffice
- **React Context only** — insufficient for server state caching and loading/error management

## 6. Backend API Gap Analysis

**Decision**: Extend the PATCH endpoints to support `NotUsed` and `NotUsedReason` fields, per FR-006.

**Rationale**: The current `UpdateEntityDescriptionRequest` record only contains `Description` and `SemanticDescription`. The spec requires editing `NotUsed` flag and `NotUsedReason`. The request model needs to be extended:

```csharp
public record UpdateEntityDescriptionRequest(
    string? Description,
    string? SemanticDescription,
    bool? NotUsed,
    string? NotUsedReason
);
```

The PATCH handlers in `TableEndpoints.cs`, `ViewEndpoints.cs`, and `StoredProcedureEndpoints.cs` need corresponding updates to apply these fields.

**Column-level PATCH**: The current API does not support column-level description updates. A new endpoint is needed:

- `PATCH /api/tables/{schema}/{name}/columns/{columnName}` — update column Description and SemanticDescription

**View column PATCH**: Similarly for views:

- `PATCH /api/views/{schema}/{name}/columns/{columnName}`

**Stored procedure parameter PATCH**: Not required in initial scope (FR-007 only mentions tables and views for column-level editing).

## 7. Routing Strategy

**Decision**: Use React Router v7 with the following route structure:

- `/` — Dashboard (model overview)
- `/tables` — Tables list
- `/tables/:schema/:name` — Table detail
- `/views` — Views list
- `/views/:schema/:name` — View detail
- `/stored-procedures` — Stored procedures list
- `/stored-procedures/:schema/:name` — Stored procedure detail

**Rationale**: React Router is the standard routing library for React SPAs. The route structure mirrors the API path structure for consistency. Schema and name as URL parameters enable direct linking to specific entities.

**Alternatives considered**:

- **TanStack Router** — newer, less ecosystem support
- **Flat routes** — `/:type/:schema/:name` would reduce route definitions but obscure intent

## 8. Testing Strategy

**Decision**: Use Vitest for unit tests, React Testing Library for component tests, and Playwright for E2E tests.

**Rationale**:

- **Vitest** — native Vite integration, fast, compatible with the build toolchain
- **React Testing Library** — standard for testing React components, focuses on user behavior
- **Playwright** — already in the project's MCP configuration, provides reliable cross-browser E2E testing

**Alternatives considered**:

- **Jest** — requires additional configuration for Vite projects; Vitest is the Vite-native alternative
- **Cypress** — heavier than Playwright, not already in the project toolchain
