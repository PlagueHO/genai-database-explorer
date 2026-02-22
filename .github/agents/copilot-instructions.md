# genai-database-explorer Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-20

## Active Technologies
- .NET 10 / C# 14 + `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, `Scriban`, `YamlDotNet` (existing), `Microsoft.SemanticKernel.Connectors.InMemory` (retained — no SK core dependency) (001-migrate-agent-framework)
- SQL Server (existing connection), LocalDisk/AzureBlob/CosmosDB repositories (unchanged) (001-migrate-agent-framework)
- .NET 10 / C# 14 + `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, `Azure.AI.Projects` (Microsoft Foundry SDK), `Azure.AI.Projects.OpenAI` (Foundry OpenAI integration), `Scriban`, `YamlDotNet` (existing), `Microsoft.SemanticKernel.Connectors.InMemory` (retained — no SK core dependency) (001-migrate-agent-framework)
- C# 14 / .NET 10 + `Azure.AI.OpenAI` 2.1.0, `Microsoft.Extensions.AI.OpenAI` 10.3.0, `Azure.Identity` 1.17.1, `System.CommandLine` (002-foundry-models-direct)
- JSON settings files (`settings.json` per project) (002-foundry-models-direct)

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
- 002-foundry-models-direct: Added C# 14 / .NET 10 + `Azure.AI.OpenAI` 2.1.0, `Microsoft.Extensions.AI.OpenAI` 10.3.0, `Azure.Identity` 1.17.1, `System.CommandLine`
- 002-foundry-models-direct: Added C# 14 / .NET 10 + `Azure.AI.OpenAI` 2.1.0, `Microsoft.Extensions.AI.OpenAI` 10.3.0, `Azure.Identity` 1.17.1, `System.CommandLine`
- 001-migrate-agent-framework: Added .NET 10 / C# 14 + `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, `Azure.AI.Projects` (Microsoft Foundry SDK), `Azure.AI.Projects.OpenAI` (Foundry OpenAI integration), `Scriban`, `YamlDotNet` (existing), `Microsoft.SemanticKernel.Connectors.InMemory` (retained — no SK core dependency)


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
