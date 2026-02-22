# Migration Guide: Semantic Kernel to Microsoft.Extensions.AI

This document describes the changes required when migrating from the Semantic Kernel-based architecture to
Microsoft.Extensions.AI (`IChatClient`, `IEmbeddingGenerator`, prompt template parser/renderer).

## Overview

The application previously used `ISemanticKernelFactory` / `SemanticKernelFactory` to create SK `Kernel` instances
for all AI operations (chat completion, structured output, embeddings). This has been replaced with:

| Old Component | New Component |
|---|---|
| `ISemanticKernelFactory` | `IChatClientFactory` |
| `Kernel.InvokeAsync(function, args)` | `IChatClient.GetResponseAsync(messages, options)` |
| Prompty `.prompty` files under `Core/Prompty/` | `.prompt` files under `Core/PromptTemplates/` |
| `KernelArguments` | `Dictionary<string, object?>` variables + `ILiquidTemplateRenderer` |
| `PromptExecutionSettings` (response format) | `ChatResponseFormat.ForJsonSchema<T>()` via `ChatOptions` |
| `ChatTokenUsage` from `result.Metadata` | `UsageDetails` from `ChatResponse.Usage` |
| `SemanticKernelEmbeddingGenerator` | `ChatClientEmbeddingGenerator` |
| SK NuGet packages (9 packages) | `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI` |

## Settings Changes

## NuGet Package Changes

### Removed Packages

- `Microsoft.SemanticKernel` 1.70.0
- `Microsoft.SemanticKernel.Abstractions` 1.70.0
- `Microsoft.SemanticKernel.Connectors.AzureOpenAI` 1.70.0
- `Microsoft.SemanticKernel.Connectors.OpenAI` 1.70.0
- `Microsoft.SemanticKernel.Core` 1.70.0
- `Microsoft.SemanticKernel.PromptTemplates.Handlebars` 1.70.0
- `Microsoft.SemanticKernel.PromptTemplates.Liquid` 1.70.0
- `Microsoft.SemanticKernel.Prompty` 1.68.0-beta
- `Microsoft.SemanticKernel.Yaml` 1.70.0

### Retained Package

- `Microsoft.SemanticKernel.Connectors.InMemory` 1.68.0-preview — used for in-memory vector store
  (has zero dependency on SK core packages)

### Added Packages

- `Microsoft.Extensions.AI.OpenAI` 10.3.0
- `Azure.AI.OpenAI` 2.1.0
- `Azure.AI.Projects` 1.0.0-beta.5
- `Azure.AI.Projects.OpenAI` 1.0.0-beta.5
- `Scriban` 6.2.1
- `YamlDotNet` 16.3.0

## Dependency Injection Changes

In `HostBuilderExtensions.ConfigureServices()`:

```csharp
// Removed:
services.AddSingleton<ISemanticKernelFactory, SemanticKernelFactory>();
services.AddSingleton<IEmbeddingGenerator, SemanticKernelEmbeddingGenerator>();

// Added:
services.AddSingleton<IChatClientFactory, ChatClientFactory>();
services.AddSingleton<IPromptTemplateParser, PromptTemplateParser>();
services.AddSingleton<ILiquidTemplateRenderer, LiquidTemplateRenderer>();
services.AddSingleton<IEmbeddingGenerator, ChatClientEmbeddingGenerator>();
```

## Prompt Template Migration

Prompt files moved from `Core/Prompty/*.prompty` to `Core/PromptTemplates/*.prompt`.

The new `.prompt` format uses YAML frontmatter with Liquid template syntax:

```yaml
---
name: describe_table
description: Generate a description for a database table
model:
  api: chat
  parameters:
    temperature: 0.1
    max_tokens: 2000
---
system:
You are a database documentation expert.

user:
Describe the following table: {{table_name}}
Schema: {{table_schema}}
```

Templates are parsed by `IPromptTemplateParser` and rendered by `ILiquidTemplateRenderer`.

## Token Tracking Changes

Token usage tracking changed from SK's `ChatTokenUsage` to `Microsoft.Extensions.AI.UsageDetails`:

```csharp
// Old:
var tokenUsage = result.Metadata?["Usage"] as ChatTokenUsage;
int inputTokens = tokenUsage?.InputTokenCount ?? 0;

// New:
var usage = response.Usage;
long inputTokens = usage?.InputTokenCount ?? 0L;
```

Note: Token counts are now `long` (not `int`) to accommodate larger values.
