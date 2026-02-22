# Implementation Plan: Migrate to Microsoft Foundry Models Direct

**Branch**: `002-foundry-models-direct` | **Date**: 2026-02-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-foundry-models-direct/spec.md`

## Summary

Migrate from OpenAI/Azure OpenAI-specific configuration and naming to Microsoft Foundry Models Direct. The `OpenAIService` settings section becomes `FoundryModels`, all OpenAI-prefixed C# classes/interfaces/enums are renamed to Foundry-centric equivalents, and validation logic is updated for Foundry endpoint patterns. The `Azure.AI.OpenAI` SDK remains as the internal transport layer — only wrapper naming changes. The `IChatClientFactory` interface contract is unchanged; all consumers require zero modifications.

## Technical Context

**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: `Azure.AI.OpenAI` 2.1.0, `Microsoft.Extensions.AI.OpenAI` 10.3.0, `Azure.Identity` 1.17.1, `System.CommandLine`
**Storage**: JSON settings files (`settings.json` per project)
**Testing**: MSTest + FluentAssertions + Moq (`dotnet test`)
**Target Platform**: Cross-platform CLI (.NET 10)
**Project Type**: Multi-project .NET solution (Console + Core library + Tests)
**Performance Goals**: Settings validation < 1s (SC-004)
**Constraints**: Backwards-compatible error messages for old config format (FR-011)
**Scale/Scope**: 8 files renamed, 3 files modified, 2 JSON files rewritten, 10 test files updated

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Semantic Model Integrity | PASS | No semantic model changes — only AI client configuration layer |
| II. AI Integration via Semantic Kernel | PASS | `IChatClientFactory` interface contract unchanged. Internal implementation continues using `Azure.AI.OpenAI` SDK. Note: constitution says "Semantic Kernel" but codebase has migrated to `Microsoft.Extensions.AI` — no regression. |
| III. Repository Pattern for Persistence | PASS | Not affected — persistence layer untouched |
| IV. Project-Based Workflow | PASS | Settings remain in `settings.json`, validated on load with clear errors. Migration detection added (FR-011). |
| V. Test-First Development | PASS | All 10 affected test files will be updated. Tests written/updated before implementation per AAA pattern. |
| VI. CLI-First Interface | PASS | No CLI command changes — only internal settings model |
| VII. Dependency Injection & Configuration | PASS | DI registration unchanged (`IChatClientFactory` → `ChatClientFactory`). Configuration binding updated to new `PropertyName`. |
| Code Style (NON-NEGOTIABLE) | PASS | PascalCase maintained. `dotnet format` / `format-fix-whitespace-only` task required after C# changes. |
| Security Requirements | PASS | No parameterized query changes. API key handling preserved. Managed identity remains default. |
| Naming Conventions | PASS | "CosmosDb" convention unaffected. New naming follows convention (e.g., `FoundryModelsSettings`). |

**Pre-design gate**: PASS — no violations.

### Post-Design Re-check

| Principle | Status | Notes |
|-----------|--------|-------|
| II. AI Integration | PASS | Research confirmed: `Azure.AI.OpenAI` SDK stays as transport. Only wrapper naming changes. |
| IV. Project-Based Workflow | PASS | Old `OpenAIService` detection added with actionable error message. |
| V. Test-First Development | PASS | 10 test files identified. `ChatClientFactoryTests` and `ProjectSettingsIntegrationTests` need heavy updates; 8 others are light renames. |

**Post-design gate**: PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/002-foundry-models-direct/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: SDK choice, endpoint patterns, naming decisions
├── data-model.md        # Phase 1: Entity definitions (before/after)
├── quickstart.md        # Phase 1: Migration guide for users
├── contracts/
│   └── settings-schema.md  # Phase 1: New settings JSON schema
├── checklists/
│   └── requirements.md  # Quality checklist
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (files affected by this feature)

```text
src/GenAIDBExplorer/
├── GenAIDBExplorer.Core/
│   ├── ChatClients/
│   │   ├── ChatClientFactory.cs              # MODIFY: update settings property paths
│   │   └── IChatClientFactory.cs             # UNCHANGED
│   ├── Models/Project/
│   │   ├── FoundryModelsSettings.cs          # RENAME from OpenAIServiceSettings.cs
│   │   ├── FoundryModelsDefaultSettings.cs   # RENAME from OpenAIServiceDefaultSettings.cs
│   │   ├── ChatCompletionDeploymentSettings.cs           # RENAME from OpenAIServiceChatCompletionSettings.cs
│   │   ├── ChatCompletionStructuredDeploymentSettings.cs # RENAME from OpenAIServiceChatCompletionStructuredSettings.cs
│   │   ├── EmbeddingDeploymentSettings.cs                # RENAME from OpenAIServiceEmbeddingSettings.cs
│   │   ├── AuthenticationType.cs             # RENAME from AzureOpenAIAuthenticationType.cs
│   │   ├── IChatCompletionDeploymentSettings.cs  # RENAME from IOpenAIServiceChatCompletionSettings.cs
│   │   ├── IEmbeddingDeploymentSettings.cs       # RENAME from IOpenAIServiceEmbeddingSettings.cs
│   │   ├── ProjectSettings.cs                # MODIFY: rename property + type
│   │   ├── Project.cs                        # MODIFY: initialization, binding, validation
│   │   └── RequiredOnPropertyValueAttribute.cs  # UNCHANGED (conditional validation on ServiceType removed)
│   └── DefaultProject/
│       └── settings.json                     # REWRITE: OpenAIService → FoundryModels
├── GenAIDBExplorer.Console/
│   └── Extensions/
│       └── HostBuilderExtensions.cs          # UNCHANGED (uses IChatClientFactory interface)
└── Tests/Unit/
    ├── GenAIDBExplorer.Core.Test/
    │   ├── ChatClients/ChatClientFactoryTests.cs                    # HEAVY UPDATE
    │   ├── Models/Project/ProjectSettingsIntegrationTests.cs        # HEAVY UPDATE
    │   ├── SemanticProviders/SemanticDescriptionProviderTests.cs    # LIGHT UPDATE
    │   ├── SemanticModelProviders/SemanticModelProvider.Tests.cs    # LIGHT UPDATE
    │   ├── Data/DatabaseProviders/SqlConnectionProviderTests.cs     # LIGHT UPDATE
    │   ├── DataDictionary/DataDictionaryProviderTests.cs            # LIGHT UPDATE
    │   ├── VectorEmbedding/Orchestration/VectorGenerationServiceTests.cs  # LIGHT UPDATE
    │   └── VectorEmbedding/E2E/InMemoryE2ETests.cs                 # LIGHT UPDATE
    └── GenAIDBExplorer.Console.Test/
        ├── ExtractModelCommandHandlerTests.cs                       # MODERATE UPDATE
        └── ShowObjectCommandHandlerTests.cs                         # LIGHT UPDATE

samples/AdventureWorksLT/
└── settings.json                             # REWRITE: OpenAIService → FoundryModels
```

**Structure Decision**: Existing multi-project .NET solution structure is preserved. No new projects, directories, or architectural changes. This is purely a rename/restructure within the existing `Models/Project/` namespace and `ChatClients/` namespace.

## Complexity Tracking

No constitution violations — no entries needed.
