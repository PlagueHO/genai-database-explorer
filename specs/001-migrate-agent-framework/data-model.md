# Data Model: Migrate from Semantic Kernel to Microsoft Agent Framework

**Branch**: `001-migrate-agent-framework` | **Date**: 2026-02-20

## New Entities

### PromptTemplateDefinition

Represents a fully parsed prompt template file (`.prompt`), containing extracted metadata and an ordered list of messages.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| Name | `string` | Template name from YAML frontmatter | Required, non-empty |
| Description | `string?` | Optional description from YAML frontmatter | Optional |
| ModelParameters | `PromptTemplateModelParameters` | Model parameters (temperature, etc.) from YAML frontmatter | Required, defaults applied if missing |
| Messages | `IReadOnlyList<PromptTemplateMessage>` | Ordered list of role-delimited messages | Required, at least 1 message |

### PromptTemplateModelParameters

Model configuration extracted from YAML frontmatter.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| Temperature | `double?` | Sampling temperature | Optional, 0.0–2.0 if present |
| TopP | `double?` | Nucleus sampling | Optional, 0.0–1.0 if present |
| MaxTokens | `int?` | Maximum response tokens | Optional, > 0 if present |

### PromptTemplateMessage

A single message within a prompt template.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| Role | `ChatRole` | Message role: System, User, or Assistant | Required, valid ChatRole |
| ContentTemplate | `string` | Raw Liquid template text for this message | Required (may be empty string) |

### ChatClientFactory Configuration

Reuses existing `OpenAIServiceSettings` model hierarchy with one new field:

- `OpenAIServiceSettings` → `Default`, `ChatCompletion`, `ChatCompletionStructured`, `Embedding`
- `OpenAIServiceDefaultSettings` → `ServiceType`, `AzureAuthenticationType`, endpoints, keys
- `AzureOpenAIAuthenticationType` enum → `ApiKey`, `EntraIdAuthentication`

The factory reads these settings via `IProject.Settings.OpenAIService`.

**New field** (added for Microsoft Foundry SDK alignment — spec.md clarification Q5):

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| FoundryProjectEndpoint | `string?` | Microsoft Foundry project endpoint URL (`https://<resource>.services.ai.azure.com/api/projects/<project-name>`) used by `AIProjectClient` for connection discovery | Required when using Foundry SDK; valid HTTPS URL |

This field is added to the project settings model (see tasks.md T023) and surfaced in `settings.json`.

## Modified Entities

### SemanticProcessResultItem (unchanged structure)

Token usage tracking continues using the same `SemanticProcessResultItem` record. The source of token data changes from `FunctionResult.Metadata["Usage"] as OpenAI.Chat.ChatTokenUsage` to `ChatResponse.Usage` (which contains `InputTokenCount` and `OutputTokenCount`).

No structural changes needed — the `ChatTokenUsage` type is from the OpenAI SDK which remains a transitive dependency via `Azure.AI.OpenAI`.

### EntityVectorRecord (unchanged)

The `[VectorStoreKey]`, `[VectorStoreData]`, and `[VectorStoreVector]` attributes from `Microsoft.Extensions.VectorData` are retained. These do not depend on Semantic Kernel core. The `SkInMemoryVectorIndexWriter` and `SkInMemoryVectorSearchService` (which use `InMemoryVectorStore` from the retained `Microsoft.SemanticKernel.Connectors.InMemory` package) use these attributes via the VectorData APIs.

## Deleted Entities

| Entity | File | Reason |
|--------|------|--------|
| `ISemanticKernelFactory` | `SemanticKernel/ISemanticKernelFactory.cs` | Replaced by `IChatClientFactory` |
| `SemanticKernelFactory` | `SemanticKernel/SemanticKernelFactory.cs` | Replaced by `ChatClientFactory` |
| `SemanticKernelEmbeddingGenerator` | `SemanticVectors/Embeddings/SemanticKernelEmbeddingGenerator.cs` | Replaced by `ChatClientEmbeddingGenerator` |

## Retained Entities (Originally Planned for Deletion)

> **Decision D1 (2026-02-21)**: `Microsoft.SemanticKernel.Connectors.InMemory` has no dependency on SK core. These entities are retained as-is.

| Entity | File | Reason Retained |
|--------|------|----------------|
| `SkInMemoryVectorIndexWriter` | `SemanticVectors/Indexing/SkInMemoryVectorIndexWriter.cs` | Only imports `Microsoft.SemanticKernel.Connectors.InMemory` (no SK core dep) |
| `SkInMemoryVectorSearchService` | `SemanticVectors/Search/SkInMemoryVectorSearchService.cs` | Only imports `Microsoft.SemanticKernel.Connectors.InMemory` (no SK core dep) |
| `InMemoryVectorStoreAdapter` | `SemanticVectors/Search/InMemoryVectorStoreAdapter.cs` | Only imports `Microsoft.SemanticKernel.Connectors.InMemory` (no SK core dep) |
| `IVectorStoreAdapter` | `SemanticVectors/Search/IVectorStoreAdapter.cs` | Only imports `Microsoft.SemanticKernel.Connectors.InMemory` (no SK core dep) |

## State Transitions

No state machines are introduced. The existing enrichment workflow (extract → enrich → query) remains unchanged. Only the internal implementation of the "enrich" step changes.

## Relationships

```text
ChatClientFactory ──creates──► IChatClient (for chat completion)
ChatClientFactory ──creates──► IChatClient (for structured output)
ChatClientFactory ──creates──► IEmbeddingGenerator<string, Embedding<float>>

PromptTemplateParser ──produces──► PromptTemplateDefinition
PromptTemplateDefinition ──contains──► PromptTemplateMessage[]

LiquidTemplateRenderer ──renders──► PromptTemplateMessage → ChatMessage

SemanticDescriptionProvider ──uses──► IChatClientFactory (replacing ISemanticKernelFactory)
SemanticDescriptionProvider ──uses──► IPromptTemplateParser
SemanticDescriptionProvider ──uses──► ILiquidTemplateRenderer

DataDictionaryProvider ──uses──► IChatClientFactory (replacing ISemanticKernelFactory)
DataDictionaryProvider ──uses──► IPromptTemplateParser
DataDictionaryProvider ──uses──► ILiquidTemplateRenderer
```
