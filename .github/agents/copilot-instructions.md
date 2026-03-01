# genai-database-explorer Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-20

## Active Technologies
- .NET 10 / C# 14 + `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, `Scriban`, `YamlDotNet` (existing), `Microsoft.SemanticKernel.Connectors.InMemory` (retained — no SK core dependency) (001-migrate-agent-framework)
- SQL Server (existing connection), LocalDisk/AzureBlob/CosmosDB repositories (unchanged) (001-migrate-agent-framework)
- .NET 10 / C# 14 + `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, `Azure.AI.Projects` (Microsoft Foundry SDK), `Azure.AI.Projects.OpenAI` (Foundry OpenAI integration), `Scriban`, `YamlDotNet` (existing), `Microsoft.SemanticKernel.Connectors.InMemory` (retained — no SK core dependency) (001-migrate-agent-framework)
- C# 14 / .NET 10 + `Azure.AI.OpenAI` 2.1.0, `Microsoft.Extensions.AI.OpenAI` 10.3.0, `Azure.Identity` 1.17.1, `System.CommandLine` (002-foundry-models-direct)
- JSON settings files (`settings.json` per project) (002-foundry-models-direct)
- .NET 10 / C# 14 + ASP.NET Core Minimal APIs, `GenAIDBExplorer.Core`, `GenAIDBExplorer.ServiceDefaults` (003-api-semantic-model)
- Delegates to existing `ISemanticModelRepository` (LocalDisk/AzureBlob/CosmosDB) (003-api-semantic-model)
- [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION] + [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION] (004-frontend-semantic-explorer)
- [if applicable, e.g., PostgreSQL, CoreData, files or N/A] (004-frontend-semantic-explorer)
- TypeScript 5.x targeting ES2022, React 19, Node.js 22 LTS + React 19, Vite 6, @fluentui/react-components v9, tailwindcss v4, @tanstack/react-query v5, react-router v7 (004-frontend-semantic-explorer)
- N/A (frontend consumes REST API; backend handles persistence) (004-frontend-semantic-explorer)
- Existing vector index (InMemory, CosmosDB, or Azure AI Search via `IVectorSearchService`) (005-query-model-agent)
- .NET 10 with C# 14 (primary constructors, collection expressions, records, pattern matching) (006-adopt-foundry-project-endpoint)
- JSON (`settings.json` per project, `semanticmodel.json`), multiple persistence strategies (LocalDisk, AzureBlob, CosmosDB) via `ISemanticModelRepository` (006-adopt-foundry-project-endpoint)
- .NET 10 / C# 14 + ASP.NET Core Minimal API, Microsoft.Extensions.AI (`IChatClientFactory`, `IEmbeddingGenerator`), Microsoft.SemanticKernel.Connectors.InMemory (vector store) (007-api-vector-search)
- Vector indices (InMemory default, CosmosDB/Azure AI Search per settings); semantic model via `ISemanticModelRepository` (LocalDisk/AzureBlob/CosmosDB) (007-api-vector-search)

- C# 14 / .NET 10 (001-migrate-agent-framework)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# 14 / .NET 10

## Code Style

C# 14 / .NET 10: Follow standard conventions

## Recent Changes
- 007-api-vector-search: Added .NET 10 / C# 14 + ASP.NET Core Minimal API, Microsoft.Extensions.AI (`IChatClientFactory`, `IEmbeddingGenerator`), Microsoft.SemanticKernel.Connectors.InMemory (vector store)
- 006-adopt-foundry-project-endpoint: Added .NET 10 with C# 14 (primary constructors, collection expressions, records, pattern matching)
- 005-query-model-agent: Added .NET 10 / C# 14


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
