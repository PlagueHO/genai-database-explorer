# Tasks: Migrate from Semantic Kernel to Microsoft Agent Framework

**Input**: Design documents from `/specs/001-migrate-agent-framework/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included — Constitution Principle V mandates test-first development (NON-NEGOTIABLE). SC-008 requires test coverage for parser, renderer, and factory.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. User stories US5 and US6 are P2 priority but are scheduled as blocking infrastructure (Phases 3–4) because all P1 stories (US1, US2, US3) depend on the components they create.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/GenAIDBExplorer/GenAIDBExplorer.Core/`
- **Console**: `src/GenAIDBExplorer/GenAIDBExplorer.Console/`
- **Tests**: `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add new NuGet packages and create directory structure for new components

- [X] T001 Add NuGet packages `Azure.AI.Projects`, `Azure.AI.Projects.OpenAI`, `Microsoft.Extensions.AI.OpenAI`, `Scriban` to `src/GenAIDBExplorer/GenAIDBExplorer.Core/GenAIDBExplorer.Core.csproj`
- [X] T002 [P] Create directory `src/GenAIDBExplorer/GenAIDBExplorer.Core/ChatClients/`
- [X] T003 [P] Create directory `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/`

---

## Phase 2: Foundational — Prompt Template Models (Blocking Prerequisites)

**Purpose**: Core data model types that ALL user stories depend on. Must complete before any story phase.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Tests for Foundational ⚠️

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T004 [P] Unit tests for `PromptTemplateDefinition` in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/PromptTemplates/PromptTemplateDefinitionTests.cs`
- [X] T005 [P] Unit tests for `PromptTemplateMessage` in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/PromptTemplates/PromptTemplateMessageTests.cs`
- [X] T006 [P] Unit tests for `PromptTemplateModelParameters` in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/PromptTemplates/PromptTemplateModelParametersTests.cs`

### Implementation for Foundational

- [X] T007 [P] Implement `PromptTemplateDefinition` record in `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/PromptTemplateDefinition.cs` (fields: Name, Description, ModelParameters, Messages per data-model.md)
- [X] T008 [P] Implement `PromptTemplateMessage` record in `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/PromptTemplateMessage.cs` (fields: Role as ChatRole, ContentTemplate as string)
- [X] T009 [P] Implement `PromptTemplateModelParameters` record in `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/PromptTemplateModelParameters.cs` (fields: Temperature, TopP, MaxTokens)

**Checkpoint**: Foundation models ready — user story implementation can now begin

---

## Phase 3: User Story 5 — Prompt Templates Are Preserved (Priority: P2) 🎯 Blocking Infrastructure

**Goal**: Build the prompt template parser and Liquid renderer that US1, US2, and US3 all depend on. Although US5 is P2 priority, the parser/renderer are blocking prerequisites for all P1 stories.

**Independent Test**: Parse each of the 6 existing `.prompty` files and verify YAML metadata extraction and rendered output is character-for-character identical to the previous Semantic Kernel Prompty pipeline (SC-002).

### Tests for User Story 5 ⚠️

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T010 [P] [US5] Unit tests for `IPromptTemplateParser` — YAML extraction, role parsing, malformed YAML frontmatter, no role markers edge case, `FileNotFoundException` for missing file — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/PromptTemplates/PromptTemplateParserTests.cs`
- [X] T011 [P] [US5] Unit tests for `ILiquidTemplateRenderer` — variable substitution, for-loops, missing variables produce empty string, few-shot multi-role rendering, Liquid syntax errors throw `InvalidOperationException` — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/PromptTemplates/LiquidTemplateRendererTests.cs`
- [X] T012 [P] [US5] Parity tests — load each of the 6 `.prompt` files with known inputs, verify rendered output matches expected golden output (SC-002) — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/PromptTemplates/PromptTemplateParityTests.cs`

### Implementation for User Story 5

- [X] T013 [US5] Implement `IPromptTemplateParser` interface in `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/IPromptTemplateParser.cs` (per contract in `specs/001-migrate-agent-framework/contracts/IPromptTemplateParser.cs`)
- [X] T014 [US5] Implement `PromptTemplateParser` using YamlDotNet for YAML frontmatter extraction and role-delimiter parsing (`system:`, `user:`, `assistant:`) in `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/PromptTemplateParser.cs`
- [X] T015 [US5] Implement `ILiquidTemplateRenderer` interface in `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/ILiquidTemplateRenderer.cs` (per contract in `specs/001-migrate-agent-framework/contracts/ILiquidTemplateRenderer.cs`)
- [X] T016 [US5] Implement `LiquidTemplateRenderer` using Scriban `Template.ParseLiquid()` in `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/LiquidTemplateRenderer.cs`
- [X] T017 [US5] Copy 6 `.prompty` files from `src/GenAIDBExplorer/GenAIDBExplorer.Core/Prompty/` to `src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/` with `.prompt` extension — content unchanged (FR-008): `describe_semanticmodeltable.prompt`, `describe_semanticmodelview.prompt`, `describe_semanticmodelstoredprocedure.prompt`, `get_table_from_data_dictionary_markdown.prompt`, `get_tables_from_view_definition.prompt`, `get_tables_from_storedprocedure_definition.prompt`
- [X] T018 [US5] Create test data directory with golden expected outputs for parity tests in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/PromptTemplates/TestData/`

**Checkpoint**: Prompt template parser and renderer are functional and verified. All 6 templates produce identical output (SC-002).

---

## Phase 4: User Story 6 — Authentication & Chat Client Factory (Priority: P2) 🎯 Blocking Infrastructure

**Goal**: Build `ChatClientFactory` that creates `IChatClient` and `IEmbeddingGenerator` using the Microsoft Foundry Project SDK with both Entra ID and API key authentication (FR-004, FR-005, FR-006). This factory is a blocking prerequisite for US1, US2, and US3.

**Independent Test**: Create chat clients and embedding generators with both Entra ID and API key configurations and verify clients are correctly initialized (SC-006).

### Tests for User Story 6 ⚠️

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T019 [P] [US6] Unit tests for `IChatClientFactory` — `CreateChatClient()`, `CreateStructuredOutputChatClient()`, Entra ID auth path, API key auth path, missing config throws, invalid endpoint throws — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/ChatClients/ChatClientFactoryTests.cs`
- [X] T020 [P] [US6] Unit tests for `CreateEmbeddingGenerator()` — returns configured `IEmbeddingGenerator<string, Embedding<float>>`, correct deployment used — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/ChatClients/ChatClientFactoryEmbeddingTests.cs`

### Implementation for User Story 6

- [X] T021 [US6] Implement `IChatClientFactory` interface in `src/GenAIDBExplorer/GenAIDBExplorer.Core/ChatClients/IChatClientFactory.cs` (per contract in `specs/001-migrate-agent-framework/contracts/IChatClientFactory.cs`)
- [X] T022 [US6] Implement `ChatClientFactory` in `src/GenAIDBExplorer/GenAIDBExplorer.Core/ChatClients/ChatClientFactory.cs` — use `AIProjectClient` from `Azure.AI.Projects` with Foundry project endpoint for connection discovery, create `AzureOpenAIClient` via `Azure.AI.Projects.OpenAI`, produce `IChatClient` (via `.GetChatClient().AsIChatClient()`) and `IEmbeddingGenerator` (via `.GetEmbeddingClient().AsEmbeddingGenerator()`). Support both Entra ID (`DefaultAzureCredential`) and API key auth. Apply retry policies (10 retries, HTTP 429/5xx per FR-012).
- [X] T023 [US6] Update project settings model to include Microsoft Foundry project endpoint field (`FoundryProjectEndpoint`) in `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/` and integrate into `ProjectSettings` for deserialization from `settings.json`

**Checkpoint**: `ChatClientFactory` creates working AI clients from project settings with both auth methods (SC-006).

---

## Phase 5: User Story 1 — AI-Enriched Database Schema Descriptions (Priority: P1) 🎯 MVP

**Goal**: Migrate `SemanticDescriptionProvider` from Semantic Kernel to `IChatClient` + prompt template parser/renderer. All enrichment operations (`enrich-model`) continue working identically (FR-001, FR-010).

**Independent Test**: Run `enrich-model` against an existing extracted semantic model and verify AI-generated descriptions are produced for all tables, views, and stored procedures with token usage logged (SC-004).

### Tests for User Story 1 ⚠️

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T024 [P] [US1] Unit tests for migrated `SemanticDescriptionProvider.UpdateSemanticDescriptionAsync` — mocked `IChatClient`, verify prompt rendered correctly via `ILiquidTemplateRenderer`, token tracking from `ChatResponse.Usage`, `ILogger.BeginScope(...)` with operation context (FR-019), edge case: empty/null AI response returns graceful error (Edge Case #4) — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/SemanticProviders/SemanticDescriptionProviderTests.cs` (update existing)
- [X] T025 [P] [US1] Unit tests for `GetTableListFromViewDefinitionAsync` and `GetTableListFromStoredProcedureDefinitionAsync` with structured output — mocked `IChatClient` returning JSON matching `TableList` schema (FR-011), edge case: response not matching expected JSON schema produces clear deserialization error (Edge Case #6) — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/SemanticProviders/SemanticDescriptionProviderStructuredOutputTests.cs`

### Implementation for User Story 1

- [X] T026 [US1] Migrate `SemanticDescriptionProvider` constructor to inject `IChatClientFactory`, `IPromptTemplateParser`, `ILiquidTemplateRenderer` instead of `ISemanticKernelFactory` in `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticProviders/SemanticDescriptionProvider.cs`
- [X] T027 [US1] Rewrite `UpdateSemanticDescriptionAsync` to use `IChatClient.GetResponseAsync()` with rendered messages from parser/renderer, extract token usage from `ChatResponse.Usage`, wrap in `ILogger.BeginScope(...)` with template name and model deployment (FR-007, FR-019) in `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticProviders/SemanticDescriptionProvider.cs`
- [X] T028 [US1] Rewrite `GetTableListFromViewDefinitionAsync` and `GetTableListFromStoredProcedureDefinitionAsync` to use structured output via JSON schema response format with `IChatClient` (FR-011) in `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticProviders/SemanticDescriptionProvider.cs`
- [X] T029 [US1] Remove all SK imports (`using Microsoft.SemanticKernel.*`, `KernelArguments`, `PromptExecutionSettings`), `#pragma warning disable SKEXP0001`/`SKEXP0040` suppressions from `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticProviders/SemanticDescriptionProvider.cs`

**Checkpoint**: `enrich-model` produces AI descriptions using `IChatClient` + prompt templates. Token tracking works (SC-004).

---

## Phase 6: User Story 2 — Data Dictionary Import (Priority: P1)

**Goal**: Migrate `DataDictionaryProvider` from Semantic Kernel to `IChatClient` with structured output for data dictionary import (FR-001, FR-011).

**Independent Test**: Run `data-dictionary` with a sample dictionary source and verify structured output is correctly parsed into `TableDataDictionary` format with token usage logged.

### Tests for User Story 2 ⚠️

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T030 [P] [US2] Unit tests for migrated `DataDictionaryProvider` — mocked `IChatClient` with JSON schema response, verify `TableDataDictionary` deserialization, token tracking from `ChatResponse.Usage`, `ILogger.BeginScope(...)` (FR-019), edge cases: empty/null AI response (Edge Case #4), response not matching expected JSON schema (Edge Case #6) — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/DataDictionary/DataDictionaryProviderTests.cs` (update existing or create)

### Implementation for User Story 2

- [X] T031 [US2] Migrate `DataDictionaryProvider` constructor to inject `IChatClientFactory`, `IPromptTemplateParser`, `ILiquidTemplateRenderer` instead of `ISemanticKernelFactory` in `src/GenAIDBExplorer/GenAIDBExplorer.Core/DataDictionary/DataDictionaryProvider.cs`
- [X] T032 [US2] Rewrite AI invocation in `DataDictionaryProvider` to use `IChatClient.GetResponseAsync()` with `ChatResponseFormat.ForJsonSchema<TableDataDictionary>()`, extract token usage from `ChatResponse.Usage`, wrap in `ILogger.BeginScope(...)` (FR-007, FR-011, FR-019) in `src/GenAIDBExplorer/GenAIDBExplorer.Core/DataDictionary/DataDictionaryProvider.cs`
- [X] T033 [US2] Remove all SK imports (`using Microsoft.SemanticKernel.*`, `KernelArguments`, `OpenAIPromptExecutionSettings`), `#pragma warning disable SKEXP*` from `src/GenAIDBExplorer/GenAIDBExplorer.Core/DataDictionary/DataDictionaryProvider.cs`

**Checkpoint**: `data-dictionary` imports structured AI output using `IChatClient` (SC-003). Token tracking works.

---

## Phase 7: User Story 3 — Vector Embedding Generation (Priority: P1)

**Goal**: Replace `SemanticKernelEmbeddingGenerator` with `ChatClientEmbeddingGenerator` that uses `IEmbeddingGenerator` from `IChatClientFactory` (FR-005). InMemory vector store wrappers are retained unchanged (Decision D1, FR-018).

**Independent Test**: Run `generate-vectors` against an enriched semantic model and verify vector embeddings are produced in the same format and dimensionality.

### Tests for User Story 3 ⚠️

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T034 [P] [US3] Unit tests for `ChatClientEmbeddingGenerator` — mocked `IEmbeddingGenerator<string, Embedding<float>>`, verify embedding result, `ILogger.BeginScope(...)` (FR-019), error handling for null/empty input — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/VectorEmbedding/ChatClientEmbeddingGeneratorTests.cs`

### Implementation for User Story 3

- [X] T035 [US3] Implement `ChatClientEmbeddingGenerator` in `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Embeddings/ChatClientEmbeddingGenerator.cs` — inject `IChatClientFactory`, call `CreateEmbeddingGenerator()`, wrap in `ILogger.BeginScope(...)` (FR-005, FR-019)
- [X] T036 [US3] Delete `SemanticKernelEmbeddingGenerator` from `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticVectors/Embeddings/SemanticKernelEmbeddingGenerator.cs`

**Checkpoint**: `generate-vectors` produces embeddings using `IEmbeddingGenerator` from `IChatClientFactory`. InMemory vector store unchanged (FR-018).

---

## Phase 8: User Story 4 — CLI Interface & DI Wiring (Priority: P2)

**Goal**: Update dependency injection in `HostBuilderExtensions` to register new services, remove SK registrations. Verify all CLI commands work unchanged (FR-009, FR-015).

**Independent Test**: Run each CLI command (`init-project`, `extract-model`, `enrich-model`, `query-model`, `export-model`, `data-dictionary`, `generate-vectors`, `show-object`) with same arguments and verify identical behavior (SC-003).

### Tests for User Story 4 ⚠️

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T037 [P] [US4] Unit tests for DI container — verify `IChatClientFactory`, `IPromptTemplateParser`, `ILiquidTemplateRenderer`, `IEmbeddingGenerator` resolve correctly from service provider — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/ChatClients/DependencyInjectionTests.cs`

### Implementation for User Story 4

- [X] T038 [US4] Register `IChatClientFactory`/`ChatClientFactory`, `IPromptTemplateParser`/`PromptTemplateParser`, `ILiquidTemplateRenderer`/`LiquidTemplateRenderer` as singletons (FR-015) in `src/GenAIDBExplorer/GenAIDBExplorer.Console/Extensions/HostBuilderExtensions.cs`
- [X] T039 [US4] Register `ChatClientEmbeddingGenerator` replacing `SemanticKernelEmbeddingGenerator` for embedding generation in `src/GenAIDBExplorer/GenAIDBExplorer.Console/Extensions/HostBuilderExtensions.cs`
- [X] T040 [US4] Remove `ISemanticKernelFactory`/`SemanticKernelFactory` singleton registration from `src/GenAIDBExplorer/GenAIDBExplorer.Console/Extensions/HostBuilderExtensions.cs`
- [X] T041 [US4] Update `settings.json` sample to include Microsoft Foundry project endpoint field in `samples/AdventureWorksLT/settings.json`

**Checkpoint**: All CLI commands execute with new DI registrations (SC-003). No SK factory in DI container.

---

## Phase 9: User Story 7 — Semantic Kernel Dependency Removal (Priority: P3)

**Goal**: Delete all remaining SK code, remove SK packages from `.csproj` files, verify zero SK references remain except `Microsoft.SemanticKernel.Connectors.InMemory` (FR-013, FR-014, SC-005).

**Independent Test**: Search all `.csproj` files for SK packages and all `.cs` files for SK namespaces — only InMemory connector references should remain.

### Tests for User Story 7 ⚠️

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T042 [P] [US7] Verification test — scan all `.cs` files for `using Microsoft.SemanticKernel` (excluding `Connectors.InMemory` in vector store files), `KernelArguments`, `PromptExecutionSettings`, `SKEXP` — in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/Migration/SemanticKernelRemovalVerificationTests.cs`

### Implementation for User Story 7

- [X] T043 [P] [US7] Delete `ISemanticKernelFactory` from `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticKernel/ISemanticKernelFactory.cs` (FR-014)
- [X] T044 [P] [US7] Delete `SemanticKernelFactory` from `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticKernel/SemanticKernelFactory.cs` (FR-014)
- [X] T045 [US7] Delete `SemanticKernel/` directory from `src/GenAIDBExplorer/GenAIDBExplorer.Core/SemanticKernel/`
- [X] T046 [US7] Delete old `Prompty/` directory from `src/GenAIDBExplorer/GenAIDBExplorer.Core/Prompty/` (templates already copied to `PromptTemplates/` in T017)
- [X] T047 [US7] Delete `SemanticKernelFactoryTests.cs` from `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/SemanticKernel/SemanticKernelFactoryTests.cs`
- [X] T048 [US7] Remove 9 SK NuGet packages from `src/GenAIDBExplorer/GenAIDBExplorer.Core/GenAIDBExplorer.Core.csproj` — retain `Microsoft.SemanticKernel.Connectors.InMemory` per FR-018, Decision D1 (packages to remove: `Microsoft.SemanticKernel`, `Microsoft.SemanticKernel.Abstractions`, `Microsoft.SemanticKernel.Connectors.AzureOpenAI`, `Microsoft.SemanticKernel.Connectors.OpenAI`, `Microsoft.SemanticKernel.Core`, `Microsoft.SemanticKernel.PromptTemplates.Handlebars`, `Microsoft.SemanticKernel.PromptTemplates.Liquid`, `Microsoft.SemanticKernel.Prompty`, `Microsoft.SemanticKernel.Yaml`)
- [X] T049 [US7] Remove any SK NuGet packages (except `Microsoft.SemanticKernel.Connectors.InMemory`) from `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/GenAIDBExplorer.Core.Test.csproj`
- [X] T050 [US7] Verify solution builds with `dotnet build src/GenAIDBExplorer/GenAIDBExplorer.slnx`
- [X] T051 [US7] Run full test suite with `dotnet test --solution src/GenAIDBExplorer/GenAIDBExplorer.slnx` — all tests must pass (SC-001)

**Checkpoint**: Zero SK packages remain except InMemory connector (SC-005). Solution builds and all tests pass (SC-001).

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, constitution amendment, settings migration guide, code formatting

- [X] T052 [P] Write migration guide documenting new `settings.json` fields for Microsoft Foundry Project endpoint (FR-017) in `docs/technical/migration-agent-framework.md`
- [X] T053 [P] Update Constitution Principle II: (a) rename principle title from "AI Integration via Semantic Kernel" to "AI Integration via Microsoft.Extensions.AI", (b) replace `ISemanticKernelFactory.CreateSemanticKernel()` with `IChatClientFactory`, (c) update prompt file location from `.prompty` under `Core/Prompty/` to `.prompt` under `Core/PromptTemplates/`, (d) update token tracking from `result.Metadata?["Usage"] as ChatTokenUsage` to `ChatResponse.Usage`, (e) update enforced pattern reference from `SemanticDescriptionProvider` to migrated pattern using `IChatClient` (R9 from research.md)
- [X] T054 [P] Update `docs/components/` documentation to reflect new architecture (`ChatClientFactory`, `PromptTemplateParser`, `LiquidTemplateRenderer`)
- [X] T055 Run `dotnet format src/GenAIDBExplorer/GenAIDBExplorer.slnx whitespace` to ensure code formatting compliance
- [X] T056 Run quickstart.md validation — verify all steps in `specs/001-migrate-agent-framework/quickstart.md` execute successfully

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — can start immediately
- **Phase 2 (Foundational Models)**: Depends on Phase 1 (packages installed, directories created)
- **Phase 3 (US5 — Parser/Renderer)**: Depends on Phase 2 (model types available) — **BLOCKS US1, US2, US3**
- **Phase 4 (US6 — ChatClientFactory)**: Depends on Phase 1 (packages installed) — **BLOCKS US1, US2, US3**
- **Phase 5 (US1 — Enrichment)**: Depends on Phase 3 + Phase 4 completion
- **Phase 6 (US2 — Data Dictionary)**: Depends on Phase 3 + Phase 4 completion
- **Phase 7 (US3 — Embeddings)**: Depends on Phase 4 completion (needs `IChatClientFactory` for embedding generator)
- **Phase 8 (US4 — DI/CLI)**: Depends on Phase 5 + Phase 6 + Phase 7 completion (all new services must exist)
- **Phase 9 (US7 — SK Removal)**: Depends on Phase 8 completion (all SK references replaced in providers)
- **Phase 10 (Polish)**: Depends on Phase 9 completion (everything migrated and verified)

### User Story Dependencies

```text
                    Phase 1: Setup
                   /       |       \
                  /        |        \
     Phase 2: Foundational |  Phase 4: US6
            /              |  (ChatClientFactory)
  Phase 3: US5             |        |
  (Parser/Renderer)        |        |
       |    \              /        |
       |     \              /     |
       |      +----+  +---+      |
       |           |  |          |
  Phase 5: US1  Phase 6: US2  Phase 7: US3
  (Enrichment)  (DataDict)   (Embeddings)
       \           |          /
        +----------+---------+
               |
         Phase 8: US4
         (DI/CLI Wiring)
               |
         Phase 9: US7
         (SK Removal)
               |
         Phase 10: Polish
```

### Within Each User Story

- Tests MUST be written and FAIL before implementation (Constitution Principle V)
- Interfaces before implementations
- Core logic before integration
- Story complete before moving to next phase

### Parallel Opportunities

**Phase 2** (after Phase 1):
- T004, T005, T006 — all model tests in parallel
- T007, T008, T009 — all model implementations in parallel

**Phase 3 + Phase 4** (after Phase 2):
- Phase 3 (US5) and Phase 4 (US6) can run in parallel — different files, no dependencies between them

**Phase 5 + Phase 6 + Phase 7** (after Phase 3 + 4):
- US1 (T024–T029), US2 (T030–T033), US3 (T034–T036) can all run in parallel — different provider files

**Phase 9** (within US7):
- T043, T044 — file deletions in parallel

---

## Parallel Example: After Phase 2 Completes

```text
# Launch Phase 3 (US5) and Phase 4 (US6) in parallel:

Stream A (US5 — Parser/Renderer):
  T010, T011, T012 (parallel) → T013, T015 (parallel) → T014 → T016 → T017 → T018

Stream B (US6 — Auth/Factory):
  T019, T020 (parallel) → T021 → T022 → T023
```

## Parallel Example: After Phase 3 + 4 Complete

```text
# Launch US1, US2, US3 in parallel:

Stream A (US1 — Enrichment):
  T024, T025 (parallel) → T026 → T027 → T028 → T029

Stream B (US2 — Data Dictionary):
  T030 → T031 → T032 → T033

Stream C (US3 — Embeddings):
  T034 → T035 → T036
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup
1. Complete Phase 2: Foundational models
1. Complete Phase 3: US5 (Parser/Renderer) — blocking infrastructure
1. Complete Phase 4: US6 (Auth/Factory) — blocking infrastructure
1. Complete Phase 5: US1 (Enrichment migration)
1. **STOP and VALIDATE**: Test `enrich-model` independently
1. Continue with remaining stories

### Incremental Delivery

1. Setup + Foundational → Foundation ready
1. US5 + US6 → Infrastructure ready (parser, renderer, factory all working)
1. US1 → Enrichment working → Validate (MVP!)
1. US2 → Data dictionary working → Validate
1. US3 → Embeddings working → Validate
1. US4 → DI wiring, all CLI commands verified
1. US7 → SK packages removed, clean codebase
1. Polish → Documentation, constitution amendment, formatting

### Parallel Team Strategy

With multiple developers after Phase 2 completes:
- Developer A: Phase 3 (US5 — Parser/Renderer)
- Developer B: Phase 4 (US6 — Auth/Factory)

After Phase 3+4 complete:
- Developer A: Phase 5 (US1 — Enrichment)
- Developer B: Phase 6 (US2 — Data Dictionary)
- Developer C: Phase 7 (US3 — Embeddings)

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Tests MUST fail before implementing (Constitution Principle V — NON-NEGOTIABLE)
- Commit after each task or logical group
- After any `.cs` file changes, run VS Code task `format-fix-whitespace-only`
- `Microsoft.SemanticKernel.Connectors.InMemory` is explicitly RETAINED (Decision D1 — no SK core dependency)
- InMemory vector store files (`SkInMemoryVectorIndexWriter`, `SkInMemoryVectorSearchService`, `InMemoryVectorStoreAdapter`, `IVectorStoreAdapter`) are NOT modified
- Microsoft Foundry SDK (`Azure.AI.Projects` + `Azure.AI.Projects.OpenAI`) replaces direct `Azure.AI.OpenAI` initialization per spec.md clarification Q5
- US5 and US6 are P2 priority but scheduled as Phases 3–4 because they produce blocking infrastructure for all P1 stories
