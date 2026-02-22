# Research: Migrate from Semantic Kernel to Microsoft Agent Framework

**Branch**: `001-migrate-agent-framework` | **Date**: 2026-02-20

## R1: Microsoft.Extensions.AI Chat Client Abstractions

**Decision**: Use `IChatClient` from `Microsoft.Extensions.AI` as the primary chat abstraction, obtained via `AIProjectClient` from the Microsoft Foundry SDK (`Azure.AI.Projects` + `Azure.AI.Projects.OpenAI`) for connection discovery and `AzureOpenAIClient` creation.

**Rationale**: `Microsoft.Extensions.AI` provides stable, framework-agnostic abstractions (`IChatClient`, `ChatMessage`, `ChatRole`, `ChatCompletion`) that decouple the application from Semantic Kernel. The Microsoft Foundry Project SDK (`Azure.AI.Projects` + `Azure.AI.Projects.OpenAI`) provides `AIProjectClient` which discovers OpenAI connections from a single Foundry project endpoint (`https://<resource>.services.ai.azure.com/api/projects/<project-name>`) and creates `AzureOpenAIClient` instances. These in turn provide `IChatClient` (via `.AsIChatClient()`) and `IEmbeddingGenerator` (via `.AsEmbeddingGenerator()`) implementations.

> **Revised (2026-02-21)**: The original decision rejected `Azure.AI.Projects` as a sole path. During spec clarification (Q5), this was reversed to adopt full Foundry SDK alignment: Azure AI Foundry is now Microsoft Foundry, and Azure OpenAI Service is accessed via Foundry Project endpoints. The `ChatClientFactory` uses `AIProjectClient` for connection discovery.

**Alternatives Considered**:

- **Keep Semantic Kernel**: Rejected — the goal is to remove SK dependency and use standard .NET AI abstractions.
- **Use Azure.AI.Projects (Foundry) exclusively**: ~~Rejected as sole path~~ → **Adopted** during spec clarification Q5. Microsoft Foundry is the primary architecture; `AIProjectClient` discovers OpenAI connections from a project endpoint.
- **Use OpenAI SDK directly (without Foundry)**: Originally partially adopted. Now secondary — `AzureOpenAIClient` is still the concrete implementation but is obtained via `AIProjectClient.GetConnection()` rather than direct initialization.

## R2: Structured Output (JSON Schema Response Format)

**Decision**: Use `IChatClient.GetResponseAsync()` with `ChatResponseFormat.ForJsonSchema()` for structured output scenarios (TableList, TableDataDictionary).

**Rationale**: `Microsoft.Extensions.AI` supports `ChatResponseFormatJson` and `ChatResponseFormatJsonSchema` via `ChatOptions.ResponseFormat`. This maps directly to the existing OpenAI structured output pattern (`ResponseFormat = typeof(T)`) used in `SemanticDescriptionProvider` and `DataDictionaryProvider`. The schema can be generated from the existing types using `JsonSchemaExporter` or `BinaryData.FromObjectAsJson`.

**Alternatives Considered**:

- **Manual JSON extraction from text**: Rejected — unreliable, requires regex parsing.
- **Function calling / tool use**: Over-engineered for simple schema enforcement.

## R3: Prompt Template File Parsing & Rendering

**Decision**: Build a custom `PromptTemplateParser` to parse YAML frontmatter + role-delimited sections, and use `Scriban.Template.ParseLiquid()` for Liquid template rendering.

**Rationale**: The 6 `.prompty` files use a simple structure: `---` YAML header `---` followed by role markers (`system:`, `user:`, `assistant:`) with Liquid templates in the body. Scriban's `ParseLiquid` mode supports the exact Liquid subset used (variable substitution and for-loops). This is the same engine used by `Prompty.Core`, ensuring behavioral parity. YamlDotNet (already in project at v16.3.0) handles YAML parsing.

**Alternatives Considered**:

- **Fluid library**: Used internally by SK's Liquid prompt templates, but Scriban has identical behavior for the subset used and is the engine behind Prompty.Core.
- **Handlebars.NET**: Different template syntax; would require rewriting templates.
- **Keep SK Prompty loader**: Rejected — retains SK dependency.

## R4: Embedding Generator Migration

**Decision**: Use `IEmbeddingGenerator<string, Embedding<float>>` from `Microsoft.Extensions.AI` directly, created by `AzureOpenAIClient.GetEmbeddingClient().AsEmbeddingGenerator()`.

**Rationale**: The current `SemanticKernelEmbeddingGenerator` resolves `IEmbeddingGenerator` from the SK kernel's DI container. Post-migration, the `ChatClientFactory` will directly create an `IEmbeddingGenerator` from the OpenAI client, eliminating the SK intermediary. The `Microsoft.Extensions.AI` `IEmbeddingGenerator<string, Embedding<float>>` interface is already the target abstraction.

**Alternatives Considered**:

- **Keep SK embedding registration**: Rejected — retains SK dependency.
- **Use Azure AI Foundry embedding endpoint**: Could work but adds Foundry dependency for embedding; direct Azure OpenAI client is simpler.

## R5: InMemory Vector Store Strategy

**Decision**: Retain `Microsoft.SemanticKernel.Connectors.InMemory` as a standalone package dependency. Do NOT replace the Sk* InMemory vector implementations.

**Rationale**: Research (2026-02-21) confirmed that `Microsoft.SemanticKernel.Connectors.InMemory` has **zero dependency on Semantic Kernel core libraries**. Its .NET 10 target dependencies are exclusively:

- `Microsoft.Extensions.AI.Abstractions` >= 10.2.0
- `Microsoft.Extensions.DependencyInjection.Abstractions` >= 10.0.2
- `Microsoft.Extensions.VectorData.Abstractions` >= 10.0.0
- `System.Numerics.Tensors` >= 10.0.2

The NuGet README explicitly states: _"This package can be used with Semantic Kernel or independently and does not depend on any Semantic Kernel abstractions or core libraries."_

Verified in codebase: `SkInMemoryVectorIndexWriter`, `SkInMemoryVectorSearchService`, `InMemoryVectorStoreAdapter`, and `IVectorStoreAdapter` import ONLY `Microsoft.SemanticKernel.Connectors.InMemory` — no SK core imports, no SKEXP pragmas. They compile without SK core packages.

This eliminates the need for a DI registration swap, custom replacement implementations, or E2E test changes for the vector store component.

**Alternatives Considered**:

- **Switch to existing non-SK `InMemoryVectorIndexWriter`/`InMemoryVectorSearchService`** (81 lines total): Functional but does not implement `Microsoft.Extensions.VectorData` interfaces. Would require maintaining custom code that duplicates what the package provides.
- **Write new implementations from scratch**: Unnecessary — the existing package provides full VectorData compliance with no unwanted dependencies.
- **Use `Microsoft.Extensions.VectorData.InMemory` package**: Does not exist. No standalone Microsoft first-party InMemory VectorData package is available outside the SK connector namespace.
- **Remove in-memory vector entirely**: Rejected — needed for LocalDisk/AzureBlob persistence strategies and dev/test scenarios.

## R6: Token Usage Tracking

**Decision**: Extract token usage from `ChatCompletion.Usage` property (`ChatTokenUsage` in `Microsoft.Extensions.AI`) instead of `FunctionResult.Metadata["Usage"]`.

**Rationale**: `IChatClient.GetResponseAsync()` returns a `ChatResponse` object containing a `Usage` property with `InputTokenCount` and `OutputTokenCount`. This maps to the existing `SemanticProcessResultItem` tracking pattern. The OpenAI-specific `ChatTokenUsage` type is replaced by `Microsoft.Extensions.AI.ChatResponseUsage` (or the concrete OpenAI type wrapping it).

**Alternatives Considered**:

- **Skip token tracking**: Rejected — required by FR-007 and constitution principle II (cost management).
- **Custom HTTP middleware for token counting**: Over-engineered; the response already contains usage data.

## R7: Retry Policy / HTTP Resilience

**Decision**: Apply `Microsoft.Extensions.Http.Resilience` retry policies to the `HttpClient` used by the OpenAI client, identical to the current SK HTTP retry configuration.

**Rationale**: The current `SemanticKernelFactory` configures 10-retry resilience for HTTP 429 and 5xx via `ConfigureHttpClientDefaults`. The same pattern can be applied to the `HttpClient` injected into `AzureOpenAIClient`. `Microsoft.Extensions.Http.Resilience` (already in project at v10.2.0) provides this capability without SK.

**Alternatives Considered**:

- **Polly directly**: More verbose; `Microsoft.Extensions.Http.Resilience` wraps Polly with simpler API.
- **OpenAI SDK built-in retry**: Limited configurability compared to current 10-retry policy.

## R8: Authentication Strategy

**Decision**: Support both `DefaultAzureCredential` (Entra ID) and API key authentication for `AzureOpenAIClient`, matching current dual authentication paths.

**Rationale**: Current `SemanticKernelFactory` supports both auth types via `AzureOpenAIAuthenticationType` enum. The `AzureOpenAIClient` constructor accepts either `TokenCredential` or API key string, providing direct parity. The `OpenAIServiceSettings` configuration model remains unchanged.

**Alternatives Considered**:

- **Azure AI Foundry project-level auth only**: Would require Foundry project setup, breaking simpler deployments.
- **Drop API key support**: Rejected — some environments require API keys.

## R9: Constitution Principle II Amendment

**Decision**: Constitution Principle II ("AI Integration via Semantic Kernel") will need amendment post-migration to reference the new abstractions.

**Rationale**: The constitution currently mandates `ISemanticKernelFactory.CreateSemanticKernel()` for all AI operations. After migration, this principle should reference `IChatClientFactory` and `Microsoft.Extensions.AI` abstractions instead. The amendment should follow the constitution's own amendment process (minor version bump, update sync impact report).

**Note**: The constitution amendment is a separate deliverable from the code migration and should be done in the same PR to keep them synchronized.
