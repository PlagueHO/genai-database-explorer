# Quickstart: REST API for Semantic Model Repository

**Feature**: 003-api-semantic-model
**Date**: 2026-02-23

## Prerequisites

1. .NET 10 SDK installed
2. An initialized GenAI Database Explorer project (e.g., `samples/AdventureWorksLT/`)
3. The semantic model has been extracted (run `extract-model` command first)

## Running the API

### Option 1: Direct run

```bash
cd genai-database-explorer-service
dotnet run --project src/GenAIDBExplorer.Api/
```

The API reads its project path from `appsettings.json`:

```json
{
  "GenAIDBExplorer": {
    "ProjectPath": "D:/genaidbexp-temp/adventureworks"
  }
}
```

### Option 2: Via Aspire AppHost

```bash
cd genai-database-explorer-service
dotnet run --project src/GenAIDBExplorer.AppHost/
```

The Aspire dashboard will show the API as a resource with health status.

## Verifying the API

### Check health

```bash
curl http://localhost:5000/health
```

### Get the semantic model summary

```bash
curl http://localhost:5000/api/model
```

### List tables (paginated)

```bash
curl "http://localhost:5000/api/tables?offset=0&limit=10"
```

### Get a specific table

```bash
curl http://localhost:5000/api/tables/SalesLT/Product
```

### Update a table description

```bash
curl -X PATCH http://localhost:5000/api/tables/SalesLT/Product \
  -H "Content-Type: application/json" \
  -d '{"description": "Products available for sale", "semanticDescription": "Contains all products in the catalog"}'
```

### Reload the model

```bash
curl -X POST http://localhost:5000/api/model/reload
```

### View project info

```bash
curl http://localhost:5000/api/project
```

## Running Tests

```bash
cd genai-database-explorer-service
dotnet test --project tests/unit/GenAIDBExplorer.Api.Test/
```
