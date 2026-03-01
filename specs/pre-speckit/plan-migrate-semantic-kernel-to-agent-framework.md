---
goal: Migrate from Semantic Kernel + Prompty to Microsoft Agent Framework with custom prompt template loader
version: 1.0
date_created: 2026-02-20
last_updated: 2026-02-20
owner: GenAI Database Explorer Team
status: 'Not Started'
tags: [migration, agent-framework, semantic-kernel, prompty, foundry, breaking-changes]
---

# Introduction

![Status: Not Started](https://img.shields.io/badge/status-not%20started-lightgrey)

This plan outlines the migration of the GenAI Database Explorer application from Microsoft Semantic Kernel + Prompty to Microsoft Agent Framework. Semantic Kernel is being superseded by Agent Framework, and the Prompty integration (`Microsoft.SemanticKernel.Prompty`, `SKEXP0040`) is experimental with no path to GA.

The application's AI usage is **single-shot structured prompt completion** (not multi-turn agent conversations). Each prompt sends a system message, few-shot examples, and a templated user message, receiving a single response. This means the migration primarily involves replacing the Kernel + Prompty pipeline with `IChatClient` from `Microsoft.Extensions.AI`, while preserving the existing `.prompty` file format via a custom parser using Scriban for Liquid template rendering.

### Key Decisions

- **Prompt files retained**: All 6 prompt template files kept in their current YAML frontmatter + role-delimited + Liquid template format, but renamed from `.prompty` to `.prompt` extension
- **Folder renamed**: `Prompty/` renamed to `PromptTemplates/`
- **Custom parser over Prompty.Core**: `Prompty.Core` NuGet package (`0.2.3-beta`) is not used because it is unlikely to receive further investment. A custom ~100-150 line parser using Scriban (the same template engine `Prompty.Core` uses internally) provides equivalent functionality
- **Foundry Models Direct**: Azure AI Foundry Project SDK (`Azure.AI.Projects`) used to create `IChatClient` instances for model access
- **Bicep infrastructure**: Handled separately, not in scope for this plan

## 1. Requirements & Constraints

- **REQ-001**: Remove all Semantic Kernel package dependencies
- **REQ-002**: Replace `ISemanticKernelFactory` / `SemanticKernelFactory` with an `IChatClientFactory` using Azure AI Foundry Project SDK
- **REQ-003**: Implement a custom prompt template parser that loads `.prompt` files (YAML frontmatter + role-delimited messages + Liquid templates) and produces `Microsoft.Extensions.AI.ChatMessage[]`
- **REQ-004**: Implement a Liquid template renderer using Scriban (`Scriban.Template.ParseLiquid()`) compatible with all existing template syntax (`{{variable}}`, `{% for %}` loops)
- **REQ-005**: Migrate `SemanticDescriptionProvider` to use `IChatClient.GetResponseAsync()` instead of `kernel.InvokeAsync(function, arguments)`
- **REQ-006**: Migrate `DataDictionaryProvider` to use `IChatClient.GetResponseAsync()` with structured output
- **REQ-007**: Migrate `SemanticKernelEmbeddingGenerator` to resolve `IEmbeddingGenerator<string, Embedding<float>>` directly from DI without requiring a Kernel instance
- **REQ-008**: Rename `Prompty/` folder to `PromptTemplates/` and `.prompty` files to `.prompt`
- **REQ-009**: Retain all existing prompt file content unchanged (system prompts, few-shot examples, Liquid templates)
- **REQ-010**: Maintain all existing CLI behavior and functionality
- **REQ-011**: Maintain token usage tracking for AI operations
- **REQ-012**: Support both Entra ID and API key authentication for Azure AI Foundry
- **SEC-001**: Ensure no security regressions; continue using parameterized queries and secure credential handling
- **CON-001**: Must maintain compatibility with .NET 10 target framework
- **CON-002**: Cannot change the public CLI interface exposed to users
- **CON-003**: Microsoft Agent Framework is in preview (`1.0.0-rc1`); API may change before GA
- **GUD-001**: Follow the migration patterns from the [Agent Framework Migration Guide](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/AgentFrameworkMigration)
- **GUD-002**: Use `Microsoft.Extensions.AI` types (`IChatClient`, `ChatMessage`, `IEmbeddingGenerator`) as the primary AI abstractions
- **PAT-001**: Maintain the existing project-based workflow architecture (extract, enrich, query)

## 2. Implementation Steps

### Phase 1: Add New Dependencies and Foundation

#### Step 1: Add New NuGet Packages

Add the following packages to `GenAIDBExplorer.Core.csproj`:

- `Scriban` -- Liquid template rendering (same engine used internally by `Prompty.Core`)
- `YamlDotNet` -- YAML frontmatter parsing
- `Microsoft.Extensions.AI` -- `IChatClient`, `ChatMessage`, `IEmbeddingGenerator` abstractions
- `Microsoft.Extensions.AI.OpenAI` -- OpenAI `IChatClient` implementation
- `Azure.AI.Projects` -- Foundry Project SDK for creating chat clients and embedding generators
- `Microsoft.Agents.AI` (or `Microsoft.Agents.AI.Abstractions`) -- Agent Framework core types

#### Step 2: Rename Prompt Folder and Files

- Rename `src/GenAIDBExplorer/GenAIDBExplorer.Core/Prompty/` to `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/`
- Rename all `.prompty` files to `.prompt`:
  - `describe_semanticmodeltable.prompty` → `describe_semanticmodeltable.prompt`
  - `describe_semanticmodelview.prompty` → `describe_semanticmodelview.prompt`
  - `describe_semanticmodelstoredprocedure.prompty` → `describe_semanticmodelstoredprocedure.prompt`
  - `get_table_from_data_dictionary_markdown.prompty` → `get_table_from_data_dictionary_markdown.prompt`
  - `get_tables_from_view_definition.prompty` → `get_tables_from_view_definition.prompt`
  - `get_tables_from_storedprocedure_definition.prompty` → `get_tables_from_storedprocedure_definition.prompt`
- Update `GenAIDBExplorer.Core.csproj` `<None Update="...">` entries to reflect new paths and extension

### Phase 2: Implement Prompt Template Infrastructure

#### Step 3: Implement `PromptTemplateDefinition` Model

Create a data model representing a parsed prompt template file:

- `PromptTemplateDefinition` -- Contains frontmatter metadata (name, description, model parameters like temperature) and an ordered list of `PromptTemplateMessage` items
- `PromptTemplateMessage` -- Contains role (`system`, `user`, `assistant`), content text, and a flag indicating whether the content contains Liquid template syntax requiring rendering

#### Step 4: Implement `PromptTemplateParser`

Create a parser (~100-150 lines) that loads `.prompt` files:

1. Split file content on `---` delimiters to extract YAML frontmatter
1. Deserialize frontmatter with `YamlDotNet` to extract model parameters (temperature, name, description)
1. Split the body text on role markers (`system:`, `user:`, `assistant:`) using regex matching
1. Return a `PromptTemplateDefinition` with metadata and ordered message list
1. Register as a singleton in DI

#### Step 5: Implement `LiquidTemplateRenderer`

Create a Liquid template renderer using Scriban:

1. Accept a `PromptTemplateDefinition` and a dictionary of template arguments
1. For each message in the definition, render Liquid syntax using `Scriban.Template.ParseLiquid(content).Render(arguments)`
1. Return `Microsoft.Extensions.AI.ChatMessage[]` with correct `ChatRole` assignments
1. Handle all Liquid features currently used: `{{variable}}` substitution and `{% for item in collection %}` loops
1. Register as a singleton in DI

### Phase 3: Implement Chat Client Infrastructure

#### Step 6: Define `IChatClientFactory` Interface

Create `IChatClientFactory` to replace `ISemanticKernelFactory`:

- `IChatClient CreateChatClient(string serviceId)` -- Creates a chat client for the specified service (e.g., `"ChatCompletion"`, `"ChatCompletionStructured"`)
- `IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(string serviceId)` -- Creates an embedding generator for the specified service (e.g., `"Embeddings"`)

#### Step 7: Implement `ChatClientFactory`

Implement the factory using Azure AI Foundry Project SDK:

- Read configuration from `IProject.Settings.OpenAIService` (same settings structure)
- Support `AzureOpenAI` service type: use `Azure.AI.Projects` to create `IChatClient` via Foundry endpoint with `DefaultAzureCredential` or API key
- Support `OpenAI` service type: create `IChatClient` using OpenAI API key
- Configure HTTP retry policies for rate limiting (429) and transient errors (matching current `SemanticKernelFactory` behavior)
- Provide `IEmbeddingGenerator<string, Embedding<float>>` for embedding generation via the same Foundry/OpenAI connection
- Register as a singleton in DI

### Phase 4: Migrate Providers

#### Step 8: Migrate `SemanticDescriptionProvider`

Replace Semantic Kernel Prompty + Kernel invocation pattern:

**Current pattern:**

```csharp
var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();
var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
var result = await semanticKernel.InvokeAsync(function, arguments);
```

**New pattern:**

```csharp
var template = _promptTemplateParser.Parse(templateFilename);
var messages = _liquidTemplateRenderer.Render(template, arguments);
var chatClient = _chatClientFactory.CreateChatClient("ChatCompletion");
var response = await chatClient.GetResponseAsync(messages, new ChatOptions
{
    Temperature = template.Temperature
});
```

Specific changes:

- Update `_promptyFolder` constant from `"Prompty"` to `"PromptTemplates"`
- Update filename suffix from `.prompty` to `.prompt`
- Replace `KernelArguments` with `Dictionary<string, object>` for template arguments
- Replace `PromptExecutionSettings` / `OpenAIPromptExecutionSettings` with `ChatOptions`
- Continue tracking token usage from `response.Usage` (map to existing `SemanticProcessResultItem`)
- Remove all `#pragma warning disable SKEXP0040` / `SKEXP0001` / `SKEXP0010` suppressions

#### Step 9: Migrate `SemanticDescriptionProvider` Structured Output Methods

For `GetTableListFromViewDefinitionAsync` and `GetTableListFromStoredProcedureDefinitionAsync`:

- Replace `OpenAIPromptExecutionSettings { ResponseFormat = typeof(TableList) }` with `ChatOptions` using `ChatResponseFormatJson` with the appropriate JSON schema
- Use `ChatCompletionStructured` service ID via `_chatClientFactory.CreateChatClient("ChatCompletionStructured")`
- Deserialize the JSON response to `TableList` as before

#### Step 10: Migrate `DataDictionaryProvider`

Apply the same pattern as Steps 8-9:

- Replace `_semanticKernelFactory.CreateSemanticKernel()` + `CreateFunctionFromPromptyFile` + `InvokeAsync` with `PromptTemplateParser` + `LiquidTemplateRenderer` + `IChatClient.GetResponseAsync()`
- Structured output for `TableDataDictionary` uses `ChatResponseFormatJson` in `ChatOptions`
- Update `_promptyFolder` and filename suffix

#### Step 11: Migrate `SemanticKernelEmbeddingGenerator`

Simplify to resolve `IEmbeddingGenerator<string, Embedding<float>>` from `IChatClientFactory` or directly from DI:

- Remove dependency on `ISemanticKernelFactory`
- Replace `_semanticKernelFactory.CreateSemanticKernel()` + `kernel.GetRequiredService<IEmbeddingGenerator>()` with direct `IChatClientFactory.CreateEmbeddingGenerator(serviceId)` call
- Consider renaming class to `FoundryEmbeddingGenerator` or similar

### Phase 5: Update DI and Configuration

#### Step 12: Update `HostBuilderExtensions`

Update dependency injection registrations in `ConfigureHost()`:

- Remove: `services.AddSingleton<ISemanticKernelFactory, SemanticKernelFactory>()`
- Add: `services.AddSingleton<IChatClientFactory, ChatClientFactory>()`
- Add: `services.AddSingleton<PromptTemplateParser>()`
- Add: `services.AddSingleton<LiquidTemplateRenderer>()`
- Update `IEmbeddingGenerator` registration to use new factory
- Evaluate `Microsoft.SemanticKernel.Connectors.InMemory` usage for vector store -- determine if Agent Framework provides an alternative, or if this package can be retained independently

#### Step 13: Update Project Settings (if needed)

Review `settings.json` schema and `OpenAIServiceSettings` model classes:

- If Azure AI Foundry Project SDK requires a project endpoint (vs. individual Azure OpenAI endpoint), add new configuration properties
- Maintain backward compatibility with existing `AzureOpenAIEndpoint` configuration
- Authentication patterns (Entra ID / API key) carry forward to Foundry SDK

### Phase 6: Remove Semantic Kernel Dependencies

#### Step 14: Remove Semantic Kernel NuGet Packages

Remove from `GenAIDBExplorer.Core.csproj`:

- `Microsoft.SemanticKernel` (1.70.0)
- `Microsoft.SemanticKernel.Abstractions` (1.70.0)
- `Microsoft.SemanticKernel.Connectors.AzureOpenAI` (1.70.0)
- `Microsoft.SemanticKernel.Connectors.OpenAI` (1.70.0)
- `Microsoft.SemanticKernel.Core` (1.70.0)
- `Microsoft.SemanticKernel.PromptTemplates.Handlebars` (1.70.0)
- `Microsoft.SemanticKernel.PromptTemplates.Liquid` (1.70.0)
- `Microsoft.SemanticKernel.Prompty` (1.68.0-beta)
- `Microsoft.SemanticKernel.Yaml` (1.70.0)

Evaluate and decide for:

- `Microsoft.SemanticKernel.Connectors.InMemory` (1.68.0-preview) -- used for vector store; check if it has external dependencies on SK core or if it can be retained independently

#### Step 15: Remove Old Code

- Delete `ISemanticKernelFactory` interface
- Delete `SemanticKernelFactory` class
- Remove all `using Microsoft.SemanticKernel.*` statements from migrated files
- Remove all `#pragma warning disable SKEXP*` suppressions
- Remove references to `KernelArguments`, `PromptExecutionSettings`, `OpenAIPromptExecutionSettings`

### Phase 7: Testing

#### Step 16: Update Unit Tests

- Update `SemanticKernelFactoryTests` → rename/refactor to test `ChatClientFactory`
- Update `SemanticDescriptionProvider` test mocks from `ISemanticKernelFactory` to `IChatClientFactory`
- Update `DataDictionaryProvider` test mocks similarly
- Update `SemanticKernelEmbeddingGeneratorTests` for new factory pattern
- Remove `Microsoft.SemanticKernel` package from test project `GenAIDBExplorer.Core.Test.csproj`

#### Step 17: Add New Unit Tests

- `PromptTemplateParserTests` -- Test YAML frontmatter extraction, role marker splitting, edge cases (multiple user/assistant pairs, missing roles, empty content)
- `LiquidTemplateRendererTests` -- Test variable substitution, `{% for %}` loops, nested property access (`{{table.schema}}`), missing variables, empty collections
- `ChatClientFactoryTests` -- Test creation with AzureOpenAI/OpenAI config, Entra ID/API key auth, missing configuration handling

#### Step 18: Run Full Test Suite and Validate

- Run all unit tests: `dotnet test --solution .\src\GenAIDBExplorer\GenAIDBExplorer.slnx`
- Run integration tests if Azure resources are available
- Validate all CLI commands produce expected output
- Run `dotnet format` to ensure code formatting compliance

## 3. Alternatives

- **ALT-001**: Use `Prompty.Core` NuGet package (`0.2.3-beta`) directly -- Provides identical load/render/parse functionality out of the box, but is beta with uncertain future investment. Not recommended.
- **ALT-002**: Use `Fluid.Core` instead of `Scriban` for Liquid rendering -- Both are viable; Scriban chosen because it is the same engine `Prompty.Core` uses, ensuring exact template compatibility.
- **ALT-003**: Convert `.prompty` files to a new YAML-based format -- Would require rewriting all 6 prompt files. Unnecessary since the current format works and the custom parser handles it.
- **ALT-004**: Split each prompt into multiple files per role (folder-per-prompt) -- Increases file count from 6 to ~24 without clear benefit. Not recommended.
- **ALT-005**: Keep Semantic Kernel for prompt execution and only use Agent Framework for future agent features -- Creates dual dependency and doesn't achieve migration goal. Not recommended.
- **ALT-006**: Use `ChatClientAgent` (AF) with instructions per invocation -- Over-engineered for single-shot completion use case; Agent abstraction is designed for multi-turn conversations.

## 4. Dependencies

- **DEP-001**: .NET 10 SDK
- **DEP-002**: `Microsoft.Extensions.AI` package (stable)
- **DEP-003**: `Microsoft.Extensions.AI.OpenAI` package
- **DEP-004**: `Azure.AI.Projects` package (Foundry Project SDK)
- **DEP-005**: `Microsoft.Agents.AI` or `Microsoft.Agents.AI.Abstractions` (`1.0.0-rc1`, preview)
- **DEP-006**: `Scriban` NuGet package (stable, actively maintained)
- **DEP-007**: `YamlDotNet` NuGet package (stable, widely used)
- **DEP-008**: Azure AI Foundry project with deployed OpenAI models for integration testing
- **DEP-009**: Existing project dependencies (System.CommandLine, FluentAssertions, Moq, etc.) must remain compatible

## 5. Files

### New Files

- **FILE-NEW-001**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/PromptTemplateDefinition.cs` -- Data model for parsed prompt template
- **FILE-NEW-002**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/PromptTemplateMessage.cs` -- Data model for a single message in a prompt template
- **FILE-NEW-003**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/PromptTemplateParser.cs` -- Parser for `.prompt` files
- **FILE-NEW-004**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/LiquidTemplateRenderer.cs` -- Liquid template renderer using Scriban
- **FILE-NEW-005**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/ChatClient/IChatClientFactory.cs` -- Interface replacing `ISemanticKernelFactory`
- **FILE-NEW-006**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/ChatClient/ChatClientFactory.cs` -- Implementation using Foundry/OpenAI SDK
- **FILE-NEW-007**: `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/PromptTemplates/PromptTemplateParserTests.cs` -- Unit tests for parser
- **FILE-NEW-008**: `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/PromptTemplates/LiquidTemplateRendererTests.cs` -- Unit tests for renderer
- **FILE-NEW-009**: `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/ChatClient/ChatClientFactoryTests.cs` -- Unit tests for factory

### Renamed/Moved Files

- **FILE-REN-001**: `Prompty/describe_semanticmodeltable.prompty` → `PromptTemplates/describe_semanticmodeltable.prompt`
- **FILE-REN-002**: `Prompty/describe_semanticmodelview.prompty` → `PromptTemplates/describe_semanticmodelview.prompt`
- **FILE-REN-003**: `Prompty/describe_semanticmodelstoredprocedure.prompty` → `PromptTemplates/describe_semanticmodelstoredprocedure.prompt`
- **FILE-REN-004**: `Prompty/get_table_from_data_dictionary_markdown.prompty` → `PromptTemplates/get_table_from_data_dictionary_markdown.prompt`
- **FILE-REN-005**: `Prompty/get_tables_from_view_definition.prompty` → `PromptTemplates/get_tables_from_view_definition.prompt`
- **FILE-REN-006**: `Prompty/get_tables_from_storedprocedure_definition.prompty` → `PromptTemplates/get_tables_from_storedprocedure_definition.prompt`

### Modified Files

- **FILE-MOD-001**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/GenAIDBExplorer.Core.csproj` -- Update NuGet packages, update prompt file copy-to-output paths
- **FILE-MOD-002**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticProviders/SemanticDescriptionProvider.cs` -- Replace SK + Prompty invocation with IChatClient + template parser
- **FILE-MOD-003**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/DataDictionary/DataDictionaryProvider.cs` -- Same pattern change
- **FILE-MOD-004**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Embeddings/SemanticKernelEmbeddingGenerator.cs` -- Replace kernel-based embedding with direct IEmbeddingGenerator
- **FILE-MOD-005**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/Extensions/HostBuilderExtensions.cs` -- Update DI registrations
- **FILE-MOD-006**: `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/GenAIDBExplorer.Core.Test.csproj` -- Update test project packages
- **FILE-MOD-007**: `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/SemanticKernel/SemanticKernelFactoryTests.cs` -- Refactor to test ChatClientFactory
- **FILE-MOD-008**: `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/VectorEmbedding/Services/SemanticKernelEmbeddingGeneratorTests.cs` -- Update for new factory pattern

### Deleted Files

- **FILE-DEL-001**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticKernel/ISemanticKernelFactory.cs`
- **FILE-DEL-002**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticKernel/SemanticKernelFactory.cs`

## 6. Prompt Template File Inventory

All 6 prompt template files are retained with content unchanged. Only path and extension change.

| File | Role Messages | Liquid Variables | Liquid Loops |
|---|---|---|---|
| `describe_semanticmodeltable.prompt` | system, user (example), assistant (example), user (template) | `project_description`, `entity_structure`, `entity_data` | none |
| `describe_semanticmodelview.prompt` | system, user (example), assistant (example), user (template) | `project_description`, `entity_structure`, `entity_data` | `{% for table in tables %}` |
| `describe_semanticmodelstoredprocedure.prompt` | system, user (example), assistant (example), user (template) | `project_description`, `entity_definition`, `entity_parameters` | `{% for table in tables %}` |
| `get_table_from_data_dictionary_markdown.prompt` | system, user (template) | `entity_markdown` | none |
| `get_tables_from_view_definition.prompt` | system, user (template) | `entity_definition` | none |
| `get_tables_from_storedprocedure_definition.prompt` | system, user (template) | `entity_definition` | none |

## 7. Testing

- **TEST-001**: `PromptTemplateParserTests` -- Verify YAML frontmatter extraction (name, temperature, description)
- **TEST-002**: `PromptTemplateParserTests` -- Verify role marker splitting produces correct message count and roles
- **TEST-003**: `PromptTemplateParserTests` -- Verify few-shot example pairs (user + assistant) are preserved correctly
- **TEST-004**: `PromptTemplateParserTests` -- Verify edge cases (no frontmatter, empty content, single role)
- **TEST-005**: `LiquidTemplateRendererTests` -- Verify `{{variable}}` substitution with string values
- **TEST-006**: `LiquidTemplateRendererTests` -- Verify `{% for item in collection %}` loop rendering
- **TEST-007**: `LiquidTemplateRendererTests` -- Verify nested property access (`{{table.schema}}`, `{{table.name}}`)
- **TEST-008**: `LiquidTemplateRendererTests` -- Verify output is `ChatMessage[]` with correct `ChatRole` assignments
- **TEST-009**: `LiquidTemplateRendererTests` -- Verify missing variables produce empty strings (Liquid default behavior)
- **TEST-010**: `ChatClientFactoryTests` -- Verify creation with Azure OpenAI configuration
- **TEST-011**: `ChatClientFactoryTests` -- Verify creation with OpenAI configuration
- **TEST-012**: `ChatClientFactoryTests` -- Verify Entra ID authentication path
- **TEST-013**: `ChatClientFactoryTests` -- Verify API key authentication path
- **TEST-014**: `ChatClientFactoryTests` -- Verify missing configuration throws appropriate exceptions
- **TEST-015**: Existing `SemanticDescriptionProvider` tests updated to mock `IChatClientFactory` and verify end-to-end prompt rendering + chat completion flow
- **TEST-016**: Existing `DataDictionaryProvider` tests updated similarly
- **TEST-017**: Existing `SemanticKernelEmbeddingGenerator` tests updated for new factory
- **TEST-018**: Full test suite passes: `dotnet test --solution .\src\GenAIDBExplorer\GenAIDBExplorer.slnx`
- **TEST-019**: Integration tests pass with Azure AI Foundry deployed models
- **TEST-020**: All CLI commands (`extract-model`, `enrich-model`, `query-model`, `export-model`, `data-dictionary`) produce expected output

## 8. Risks & Assumptions

- **RISK-001**: Microsoft Agent Framework is preview (`1.0.0-rc1`); API may change before GA. Mitigation: The application primarily uses `Microsoft.Extensions.AI` types (`IChatClient`, `ChatMessage`) which are stable; Agent Framework types are used minimally.
- **RISK-002**: Scriban's Liquid-compatible mode may have subtle differences from the Liquid rendering used by Semantic Kernel's `PromptTemplates.Liquid` package (which uses `Fluid`). Mitigation: `Prompty.Core` itself uses Scriban for Liquid rendering, and the existing templates use only basic features (`{{}}` substitution and `{% for %}` loops). Test all templates during migration.
- **RISK-003**: Azure AI Foundry Project SDK (`Azure.AI.Projects`) configuration model may differ from current `OpenAIServiceSettings`. Mitigation: Design `ChatClientFactory` to accept the existing settings structure and map internally.
- **RISK-004**: `Microsoft.SemanticKernel.Connectors.InMemory` for vector store may have a hard dependency on SK core packages. Mitigation: Evaluate during Step 14; if dependent, find alternative in-memory vector store or extract the needed functionality.
- **RISK-005**: Token usage tracking format may differ between SK `ChatTokenUsage` and `Microsoft.Extensions.AI` response metadata. Mitigation: Map new usage format to existing `SemanticProcessResultItem` during Step 8.
- **ASSUMPTION-001**: All 6 prompt templates produce identical rendered output with Scriban's `ParseLiquid()` as they do with Fluid/SK Liquid renderer
- **ASSUMPTION-002**: `Microsoft.Extensions.AI.IChatClient` supports structured output (JSON schema response format) needed for `TableList` and `TableDataDictionary` deserialization
- **ASSUMPTION-003**: Azure AI Foundry Project SDK provides `IChatClient` and `IEmbeddingGenerator` implementations compatible with Foundry Models Direct
- **ASSUMPTION-004**: Existing `settings.json` configuration can be adapted to the new SDK without user-facing changes, or any changes are minimal and documented
- **ASSUMPTION-005**: The `InMemoryVectorStore` from SK Connectors can either be retained independently or replaced with an equivalent

## 9. Related Specifications / Further Reading

- [Agent Framework Migration Guide](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/AgentFrameworkMigration)
- [Microsoft.Agents.AI.Abstractions NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI.Abstractions)
- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/agent-framework/overview/agent-framework-overview)
- [Agent Framework GitHub Repository](https://github.com/microsoft/agent-framework)
- [Prompty.Core Source (reference for parser/renderer patterns)](https://github.com/microsoft/prompty/tree/main/runtime/promptycs/Prompty.Core)
- [Scriban Template Engine](https://github.com/scriban/scriban)
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/ai-extensions)
- [Azure AI Foundry Project SDK](https://learn.microsoft.com/en-us/azure/ai-foundry/)
