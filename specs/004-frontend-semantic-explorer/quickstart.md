# Quickstart: Frontend Semantic Model Explorer

**Feature**: `004-frontend-semantic-explorer`
**Date**: 2026-02-24

## Prerequisites

- **Node.js** 22 LTS or later
- **pnpm** (project package manager — already configured at repo root)
- **.NET 10 SDK** (for running the backend API via Aspire)
- **Aspire CLI** (`dotnet workload install aspire` if not already installed)
- A semantic model project folder with `settings.json` (e.g., `samples/AdventureWorksLT/`)

## Quick Start (Aspire — Recommended)

The frontend and backend are orchestrated together via .NET Aspire.

```bash
# From repository root
aspire run
```

This starts:

- **genaidbexplorer-api** — Backend REST API (ASP.NET Core)
- **genaidbexplorer-frontend** — Frontend SPA (React + Vite dev server)

The Aspire dashboard (opened automatically) shows both resources. Click the frontend endpoint to open the application.

## Manual Start (Development)

### 1. Start the Backend API

```bash
cd genai-database-explorer-service
dotnet run --project src/GenAIDBExplorer.Api/GenAIDBExplorer.Api.csproj
```

The API starts at `https://localhost:5001` (or the port configured in `launchSettings.json`).

### 2. Start the Frontend

```bash
cd genai-database-explorer-frontend
pnpm install
pnpm dev
```

The Vite dev server starts at `http://localhost:5173` by default.

**Environment variable**: The API base URL is configured via `VITE_API_BASE_URL`. When running via Aspire, this is injected automatically via service discovery. For manual mode, create a `.env.local` file:

```env
VITE_API_BASE_URL=https://localhost:5001
```

## Project Structure

```text
genai-database-explorer-frontend/
├── package.json
├── pnpm-lock.yaml
├── vite.config.ts
├── tsconfig.json
├── tailwind.config.ts
├── index.html
├── public/
├── src/
│   ├── main.tsx                    # App entry point
│   ├── App.tsx                     # Root component, providers, router
│   ├── api/                        # API client layer
│   │   ├── client.ts               # Base fetch wrapper
│   │   ├── projectApi.ts           # GET /api/project
│   │   ├── modelApi.ts             # GET/POST /api/model
│   │   ├── tablesApi.ts            # GET/PATCH /api/tables
│   │   ├── viewsApi.ts             # GET/PATCH /api/views
│   │   └── storedProceduresApi.ts  # GET/PATCH /api/stored-procedures
│   ├── hooks/                      # React Query hooks
│   │   ├── useProject.ts
│   │   ├── useModel.ts
│   │   ├── useTables.ts
│   │   ├── useViews.ts
│   │   └── useStoredProcedures.ts
│   ├── components/                 # Shared/reusable components
│   │   ├── layout/
│   │   │   ├── AppLayout.tsx
│   │   │   ├── Sidebar.tsx
│   │   │   └── ChatPanel.tsx
│   │   ├── common/
│   │   │   ├── SearchInput.tsx
│   │   │   ├── Pagination.tsx
│   │   │   ├── EditableField.tsx
│   │   │   ├── NotUsedEditor.tsx
│   │   │   ├── ErrorBanner.tsx
│   │   │   ├── LoadingSpinner.tsx
│   │   │   └── EmptyState.tsx
│   │   └── entities/
│   │       ├── EntityList.tsx
│   │       ├── EntityHeader.tsx
│   │       ├── ColumnsTable.tsx
│   │       ├── IndexesTable.tsx
│   │       ├── DefinitionViewer.tsx
│   │       └── ParametersDisplay.tsx
│   ├── pages/
│   │   ├── DashboardPage.tsx
│   │   ├── TablesListPage.tsx
│   │   ├── TableDetailPage.tsx
│   │   ├── ViewsListPage.tsx
│   │   ├── ViewDetailPage.tsx
│   │   ├── StoredProceduresListPage.tsx
│   │   └── StoredProcedureDetailPage.tsx
│   ├── context/
│   │   └── AppUIContext.tsx
│   └── types/
│       └── api.ts                  # TypeScript interfaces (from data-model.md)
├── tests/
│   ├── unit/                       # Vitest unit tests
│   ├── component/                  # React Testing Library tests
│   └── e2e/                        # Playwright E2E tests
└── playwright.config.ts
```

## Running Tests

```bash
cd genai-database-explorer-frontend

# Unit + component tests
pnpm test

# Unit tests in watch mode
pnpm test:watch

# E2E tests (requires backend running)
pnpm test:e2e

# Coverage report
pnpm test:coverage
```

## Key npm Scripts

| Script | Description |
|--------|-------------|
| `pnpm dev` | Start Vite dev server |
| `pnpm build` | Production build |
| `pnpm preview` | Preview production build |
| `pnpm test` | Run Vitest tests |
| `pnpm test:watch` | Run tests in watch mode |
| `pnpm test:e2e` | Run Playwright E2E tests |
| `pnpm test:coverage` | Run tests with coverage |
| `pnpm lint` | Run ESLint |
| `pnpm format` | Run Prettier |

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `react` | ^19.0 | UI framework |
| `react-dom` | ^19.0 | DOM rendering |
| `react-router` | ^7.0 | Client-side routing |
| `@tanstack/react-query` | ^5.0 | Server state management |
| `@fluentui/react-components` | ^9.0 | FluentUI component library |
| `tailwindcss` | ^4.0 | Utility-first CSS framework |

## Development Workflow

1. Start the backend (via Aspire or manually)
1. Start the frontend dev server
1. The browser opens automatically at the frontend URL
1. Changes to frontend code trigger hot module replacement (HMR)
1. API requests proxy to the backend via the configured base URL
