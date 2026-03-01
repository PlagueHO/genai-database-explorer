# Tasks: Adopt Microsoft Foundry Project Endpoint

**Input**: Design documents from `/specs/006-adopt-foundry-project-endpoint/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: Included — the constitution mandates test-first development (Principle V).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

All paths are relative to `genai-database-explorer-service/` unless otherwise noted.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: NuGet upgrades and settings model renames that all user stories depend on.

- [ ] T001 Upgrade `Azure.AI.Projects` from `1.1.0` to `1.2.0-beta.5` (or latest Foundry-new-compatible version) and `Azure.AI.Projects.OpenAI` to matching version in `src/GenAIDBExplorer.Core/GenAIDBExplorer.Core.csproj`
- [ ] T002 [P] Rename `src/GenAIDBExplorer.Core/Models/Project/FoundryModelsSettings.cs` to `MicrosoftFoundrySettings.cs` — rename class `FoundryModelsSettings` → `MicrosoftFoundrySettings`, change `PropertyName` constant from `"FoundryModels"` to `"MicrosoftFoundry"`, remove `ChatCompletionStructured` property if present
- [ ] T003 [P] Rename `src/GenAIDBExplorer.Core/Models/Project/FoundryModelsDefaultSettings.cs` to `MicrosoftFoundryDefaultSettings.cs` — rename class `FoundryModelsDefaultSettings` → `MicrosoftFoundryDefaultSettings`
- [ ] T004 Update `src/GenAIDBExplorer.Core/Models/Project/ProjectSettings.cs` — rename property `FoundryModels` → `MicrosoftFoundry`, change type from `FoundryModelsSettings` → `MicrosoftFoundrySettings`
- [ ] T005 Update all remaining references to `FoundryModelsSettings`, `FoundryModelsDefaultSettings`, and `Settings.FoundryModels` across the entire solution (fix all compilation errors from the renames in T002-T004)
- [ ] T006 Build the solution (`dotnet build GenAIDBExplorer.slnx`) and verify zero compilation errors after renames

**Checkpoint**: Solution compiles with renamed settings classes. All `FoundryModels` type references replaced with `MicrosoftFoundry` equivalents.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Settings validation and legacy detection infrastructure that ALL user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Phase 2a: Investigation (Architecture-Shaping)

- [ ] T007 [US5] Investigate Foundry Prompt Agent tool support — determine if `PromptAgentDefinition` supports function tools that call back to the application for semantic model search. If yes, proceed with Foundry-hosted agent (Option A). If no, use local `AIAgent` with Foundry-backed `IChatClient` (Option C). Document decision in `specs/006-adopt-foundry-project-endpoint/research.md` R5. **This shapes US5 test design and must be resolved before writing US5 tests.**

### Phase 2b: Tests (Write First — Constitution Principle V)

- [ ] T008 [P] Add test `ValidateSettings_LegacyFoundryModelsSection_ReturnsActionableError` in `tests/unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectTests.cs` — provide settings with `FoundryModels` section only, verify error message includes rename instructions and project endpoint format (FR-009)
- [ ] T009 [P] Add test `ValidateSettings_SettingsVersionBelowTwo_ReturnsActionableError` in `tests/unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectTests.cs` — provide settings with `SettingsVersion: "1.0.0"`, verify error includes version mismatch message (FR-009, FR-021)
- [ ] T010 [P] Add test `ValidateSettings_BothFoundryModelsAndMicrosoftFoundry_ReturnsAmbiguityError` in `tests/unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectTests.cs` — provide settings with both sections present, verify error indicates ambiguity (FR-009)
- [ ] T011 [P] Add test `ValidateSettings_OnlyMicrosoftFoundrySection_NoLegacyErrors` in `tests/unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectTests.cs` — provide valid v2.0.0 settings with only `MicrosoftFoundry`, verify no legacy warnings
- [ ] T012 [P] Add test `ValidateSettings_LegacyOpenAIServiceSection_ReturnsError` in `tests/unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectTests.cs` — verify existing `OpenAIService` legacy detection still works after refactoring (FR-010)
- [ ] T013 [P] Add endpoint validation tests in `tests/unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectTests.cs` — test `ValidateEndpoint_ValidProjectEndpoint_Passes`, `ValidateEndpoint_ResourceOnlyEndpoint_RejectsWithError`, `ValidateEndpoint_LegacyOpenAIEndpoint_RejectsWithError`, `ValidateEndpoint_LegacyCognitiveServicesEndpoint_RejectsWithError`, `ValidateEndpoint_ProjectEndpointWithTrailingSlash_Passes`, `ValidateEndpoint_NonHttpsEndpoint_RejectsWithError` (FR-003, FR-004)
- [ ] T014 [P] Add test `ValidateEndpoint_MissingDeployment_ReturnsClearError` in `tests/unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectTests.cs` — verify that a missing deployment name results in a clear error message, not a generic SDK exception (Edge Case 4)

### Phase 2c: Implementation

- [ ] T015 Update settings binding in `src/GenAIDBExplorer.Core/Models/Project/Project.cs` — change `InitializeSettings()` to bind `MicrosoftFoundry` section via `_configuration?.GetSection(MicrosoftFoundrySettings.PropertyName).Bind(Settings.MicrosoftFoundry)`, rename `ValidateFoundryModelsConfiguration()` → `ValidateMicrosoftFoundryConfiguration()`. **Preserve existing `OpenAIService` legacy detection logic intact** (FR-010)
- [ ] T016 Add legacy `FoundryModels` section detection in `src/GenAIDBExplorer.Core/Models/Project/Project.cs` — in `ValidateSettings()`, check `_configuration.GetSection("FoundryModels").Exists()` and emit actionable error: "The 'FoundryModels' configuration section has been renamed to 'MicrosoftFoundry'. Rename it in your settings.json and update the endpoint to a Foundry project endpoint format: https://<resource>.services.ai.azure.com/api/projects/<project-name>" (FR-009)
- [ ] T017 Add `SettingsVersion` validation in `src/GenAIDBExplorer.Core/Models/Project/Project.cs` — detect `SettingsVersion < 2.0.0` and emit actionable error: "Settings version {version} is no longer supported. Version 2.0.0 is required. The 'FoundryModels' section has been renamed to 'MicrosoftFoundry' and the endpoint must be a Foundry project endpoint." (FR-009, FR-021)
- [ ] T018 Add dual-section ambiguity detection in `src/GenAIDBExplorer.Core/Models/Project/Project.cs` — detect both `FoundryModels` AND `MicrosoftFoundry` sections present and emit error: "Both 'FoundryModels' and 'MicrosoftFoundry' sections found. Remove the legacy 'FoundryModels' section." (FR-009)
- [ ] T019 Add project endpoint path validation in `src/GenAIDBExplorer.Core/Models/Project/Project.cs` — within `ValidateMicrosoftFoundryConfiguration()`, validate endpoint URI: (1) HTTPS scheme required (FR-004), (2) contains `/api/projects/` followed by a non-empty segment (FR-003). Reject resource-only endpoints (`*.services.ai.azure.com/` with no project path), legacy `*.openai.azure.com` endpoints, and legacy `*.cognitiveservices.azure.com` endpoints with specific error messages for each. Accept trailing slashes.
- [ ] T020 Update `src/GenAIDBExplorer.Core/DefaultProject/settings.json` — rename `"FoundryModels"` → `"MicrosoftFoundry"`, set `"SettingsVersion": "2.0.0"`, update endpoint placeholder to `"https://<resource>.services.ai.azure.com/api/projects/<project-name>"`, remove `"ChatCompletionStructured"` sub-section if present (FR-013)
- [ ] T021 Build and run all unit tests (including new T008-T014 tests) to verify Phase 2 tests now pass and no regressions from Phase 1 + Phase 2 changes

**Checkpoint**: Foundation ready — all settings models renamed, validation enhanced, legacy detection in place, tests written and passing, solution builds.

> **Plan-to-Tasks Phase Mapping**: Plan Phase 1 (Configuration Rename & Validation) → Tasks Phases 1 + 2. Plan Phase 2 (AIProjectClient Factory) → Tasks Phase 3. Plan Phase 3 (Foundry-Hosted Agent) → Tasks Phase 7. Plan Phase 4 (Infrastructure) → Tasks Phase 6.

---

## Phase 3: User Story 1 - Connect to Microsoft Foundry via Project Endpoint (Priority: P1) 🎯 MVP

**Goal**: Replace `OpenAIClient` with `AIProjectClient` so all AI operations flow through the Foundry project endpoint.

**Independent Test**: Configure a valid Foundry project endpoint in settings, run `extract-model` then `enrich-model`, and verify chat completions and embeddings return successfully.

### Tests for User Story 1

- [ ] T022 [P] [US1] Update `tests/unit/GenAIDBExplorer.Core.Test/ChatClients/ChatClientFactoryTests.cs` — update `CreateMockProject()` helper to use `MicrosoftFoundrySettings` / `MicrosoftFoundryDefaultSettings` property names, update all test methods referencing `FoundryModels` settings
- [ ] T023 [P] [US1] Add test `CreateChatClient_WithProjectEndpoint_ShouldReturnIChatClient` in `tests/unit/GenAIDBExplorer.Core.Test/ChatClients/ChatClientFactoryTests.cs` — verify that providing a valid project endpoint (`https://test.services.ai.azure.com/api/projects/testproject`) with API key auth creates a valid `IChatClient` (FR-001, FR-006)
- [ ] T024 [P] [US1] Add test `CreateEmbeddingGenerator_WithProjectEndpoint_ShouldReturnEmbeddingGenerator` in `tests/unit/GenAIDBExplorer.Core.Test/ChatClients/ChatClientFactoryTests.cs` — verify embedding generator creation via project endpoint (FR-008)
- [ ] T025 [P] [US1] Add test `CreateChatClient_LogsProjectEndpoint` in `tests/unit/GenAIDBExplorer.Core.Test/ChatClients/ChatClientFactoryTests.cs` — verify the factory logs the project endpoint at creation time (FR-017)

### Implementation for User Story 1

- [ ] T026 [US1] Refactor `src/GenAIDBExplorer.Core/ChatClients/ChatClientFactory.cs` — remove `FoundryTokenScope` constant, remove `CreateOpenAIClient()` method, add private `CreateProjectClient()` method that creates and caches `AIProjectClient` (Entra ID via `DefaultAzureCredential`, API key via `AzureKeyCredential`), log project endpoint at creation (FR-001, FR-005, FR-006, FR-017)
- [ ] T027 [US1] Update `CreateChatClient()` in `src/GenAIDBExplorer.Core/ChatClients/ChatClientFactory.cs` — use `projectClient.OpenAI.GetProjectChatClient(deploymentName).AsIChatClient()` instead of `client.GetChatClient(deploymentName).AsIChatClient()` (FR-007)
- [ ] T028 [US1] Update `CreateEmbeddingGenerator()` in `src/GenAIDBExplorer.Core/ChatClients/ChatClientFactory.cs` — use `projectClient.OpenAI.GetProjectEmbeddingClient(deploymentName).AsIEmbeddingGenerator()` instead of `client.GetEmbeddingClient(deploymentName).AsIEmbeddingGenerator()` (FR-008)
- [ ] T029 [US1] Extend `src/GenAIDBExplorer.Core/ChatClients/IChatClientFactory.cs` — add `AIProjectClient GetProjectClient()` method to expose the project client for agent service access (required by US5), or create a separate `IFoundryProjectClientFactory` interface
- [ ] T030 [US1] Build solution and run all unit tests to validate US1 implementation

**Checkpoint**: `ChatClientFactory` creates all AI clients via `AIProjectClient`. Chat completion and embedding generation work through the Foundry project endpoint. All `ChatClientFactoryTests` pass.

---

## Phase 4: User Story 2 - Renamed Configuration Section (Priority: P2)

**Goal**: The `init-project` CLI generates settings files with the `MicrosoftFoundry` section and `SettingsVersion` 2.0.0.

**Independent Test**: Run `init-project` and verify generated settings file contains `MicrosoftFoundry` section with project endpoint format placeholder.

### Tests for User Story 2

- [ ] T031 [P] [US2] Update tests in `tests/unit/GenAIDBExplorer.Console.Test/CommandHandlers/InitProjectCommandHandlerTests.cs` — update any assertions checking for `"FoundryModels"` to check for `"MicrosoftFoundry"` instead
- [ ] T032 [P] [US2] Add test `InitProject_GeneratesSettings_WithMicrosoftFoundrySection` in `tests/unit/GenAIDBExplorer.Console.Test/CommandHandlers/InitProjectCommandHandlerTests.cs` — verify generated settings.json contains `"MicrosoftFoundry"` section (not `"FoundryModels"`), endpoint placeholder shows project format, and `"SettingsVersion"` is `"2.0.0"` (FR-011, FR-021)
- [ ] T033 [P] [US2] Add test `InitProject_FoundryEndpointOption_WritesToMicrosoftFoundrySection` in `tests/unit/GenAIDBExplorer.Console.Test/CommandHandlers/InitProjectCommandHandlerTests.cs` — verify `--foundry-endpoint` value is written to `MicrosoftFoundry.Default.Endpoint` (FR-012)
- [ ] T034 [P] [US2] Add test `InitProject_DeploymentOptions_WriteToMicrosoftFoundrySection` in `tests/unit/GenAIDBExplorer.Console.Test/CommandHandlers/InitProjectCommandHandlerTests.cs` — verify `--foundry-chat-deployment` writes to `MicrosoftFoundry.ChatCompletion.DeploymentName` and `--foundry-embedding-deployment` writes to `MicrosoftFoundry.Embedding.DeploymentName` (FR-016)

### Implementation for User Story 2

- [ ] T035 [US2] Update `src/GenAIDBExplorer.Console/CommandHandlers/InitProjectCommandHandler.cs` — change all JSON path references from `"FoundryModels"` to `"MicrosoftFoundry"` in `ApplySettingsOverrides()` and `EnsureJsonObject()` calls. Explicitly update paths for `--foundry-endpoint` (→ `MicrosoftFoundry.Default.Endpoint`), `--foundry-chat-deployment` (→ `MicrosoftFoundry.ChatCompletion.DeploymentName`), and `--foundry-embedding-deployment` (→ `MicrosoftFoundry.Embedding.DeploymentName`) (FR-012, FR-016)
- [ ] T036 [US2] Update `--foundry-endpoint` option description in `src/GenAIDBExplorer.Console/CommandHandlers/InitProjectCommandHandler.cs` — change description to reference Foundry project endpoint format (`https://<resource>.services.ai.azure.com/api/projects/<project-name>`)
- [ ] T037 [US2] Update `samples/AdventureWorksLT/settings.json` — rename `"FoundryModels"` → `"MicrosoftFoundry"`, set `"SettingsVersion": "2.0.0"`, update endpoint to project endpoint format placeholder, remove `"ChatCompletionStructured"` sub-section
- [ ] T038 [US2] Build and run all unit tests to validate US2 implementation

**Checkpoint**: `init-project` generates v2.0.0 settings with `MicrosoftFoundry` section. CLI options write to correct JSON paths. Sample project updated.

---

## Phase 5: User Story 3 - Legacy Configuration Detection and Migration Guidance (Priority: P3)

**Goal**: Users with old `FoundryModels` config or `SettingsVersion < 2.0.0` get clear, actionable migration errors.

**Independent Test**: Load a settings file with `FoundryModels` section and verify the application emits a specific error with migration instructions.

### Tests for User Story 3

Note: Core legacy detection tests were already written in Phase 2b (T008-T012). This phase adds refinement and coverage tests.

- [ ] T039 [P] [US3] Add test `ValidateSettings_LegacyFoundryModelsSection_ErrorIncludesBeforeAfterExample` in `tests/unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectTests.cs` — verify error message includes a concrete before/after JSON snippet showing `FoundryModels` → `MicrosoftFoundry` rename
- [ ] T040 [P] [US3] Add test `ValidateSettings_LegacyEndpointInMicrosoftFoundrySection_ReturnsSpecificError` in `tests/unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectTests.cs` — provide settings with `MicrosoftFoundry` section but a legacy `*.openai.azure.com` endpoint, verify error explains both the section rename AND endpoint format change (Edge Case 6)

### Implementation for User Story 3

- [ ] T041 [US3] Refine legacy detection error messages in `src/GenAIDBExplorer.Core/Models/Project/Project.cs` — ensure each error message includes: (1) what's wrong, (2) specific migration steps, (3) before/after JSON example. Covers T016-T018 messages.
- [ ] T042 [US3] Build and run all unit tests to validate US3 implementation, including all new ProjectTests

**Checkpoint**: All legacy scenarios produce clear, actionable error messages. Users with `FoundryModels` config, old `SettingsVersion`, dual sections, or `OpenAIService` sections all receive specific migration guidance.

---

## Phase 6: User Story 4 - Infrastructure Provisioning of Foundry Project (Priority: P4)

**Goal**: Bicep templates create a Foundry project and output its endpoint.

**Independent Test**: Deploy infrastructure and verify output includes a Foundry project endpoint in the correct format.

### Implementation for User Story 4

Note: Endpoint validation tests are already covered in Phase 2b (T013). This phase is Bicep-only infrastructure work.

- [ ] T043 [US4] Update `infra/main.bicep` — define `defaultProjectName` variable (e.g., `'genaidbexplorer'`), pass `projects` array with default project definition and `defaultProject` parameter to the foundry module (FR-014)
- [ ] T044 [US4] Add output `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` in `infra/main.bicep` — construct project endpoint URL: `'https://${foundryService.outputs.name}.services.ai.azure.com/api/projects/${defaultProjectName}'` (FR-015)
- [ ] T045 [US4] Validate Bicep templates compile (`bicep build infra/main.bicep`) with no errors

**Checkpoint**: Infrastructure templates create a Foundry project and output its endpoint. Bicep compiles cleanly.

---

## Phase 7: User Story 5 - Migrate Query Agent to Foundry-Hosted Agent (Priority: P5)

**Goal**: The `query-model` agent runs on the Foundry Agent Service instead of local `AsAIAgent()` orchestration.

**Independent Test**: Run `query-model` with a natural language question and verify the agent executes through Foundry agent hosting, returning correct SQL and results.

### Tests for User Story 5

Note: Tests below are written based on the T007 investigation outcome. If Option A (Foundry-hosted agent), mock `AIProjectClient.Agents`. If Option C (local agent with Foundry-backed client), mock `IChatClient` obtained from factory.

- [ ] T046 [P] [US5] Add test `CreateAgent_UsesFoundryAgentService` in `tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelQueryServiceTests.cs` — mock `AIProjectClient`, verify agent is created via Foundry Agent Service API (or local `AIAgent` with Foundry-backed `IChatClient` per T007 decision) with correct model deployment and instructions (FR-019)
- [ ] T047 [P] [US5] Add test `QueryAsync_ExecutesThroughFoundryAgent_ReturnsResult` in `tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelQueryServiceTests.cs` — verify query execution returns a result with answer text (FR-019)
- [ ] T048 [P] [US5] Add test `QueryAsync_TracksTokenUsage_AcrossRounds` in `tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelQueryServiceTests.cs` — verify token usage tracking is preserved across multi-round interactions (FR-020)
- [ ] T049 [P] [US5] Add test `QueryAsync_ProjectDoesNotSupportAgents_ReturnsError` in `tests/unit/GenAIDBExplorer.Core.Test/SemanticModelQuery/SemanticModelQueryServiceTests.cs` — verify clear error when Foundry project lacks agent hosting support (Edge Case 7)

### Implementation for User Story 5

- [ ] T050 [US5] Refactor `src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryService.cs` — update `CreateAgent()` to get `AIProjectClient` from factory (via `GetProjectClient()` or injected service), remove `OpenAI.Chat.ChatClient` unwrapping, create agent via Foundry Agent Service API or local `AIAgent` with Foundry-backed client (per T007 decision) (FR-019)
- [ ] T051 [US5] Update agent execution in `src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryService.cs` — adapt `RunAgentLoopAsync()` to use Foundry `ProjectResponsesClient` for query execution (or local `AIAgent.RunStreamingAsync` with Foundry-backed client), preserve streaming channel pattern, token tracking, guardrails (max rounds, token budget, timeout) (FR-019, FR-020)
- [ ] T052 [US5] Add Foundry agent error handling in `src/GenAIDBExplorer.Core/SemanticModelQuery/SemanticModelQueryService.cs` — handle project not supporting agents (clear error), deployment missing, authentication failures
- [ ] T053 [US5] Build and run all unit tests to validate US5 implementation

**Checkpoint**: `query-model` executes through Foundry Agent Service (or Foundry-backed local agent). Multi-round tool calling works. Token usage tracked. Errors handled gracefully.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, and formatting.

- [ ] T054 [P] Update documentation in `docs/components/` to reflect `MicrosoftFoundry` section naming, project endpoint format, and Foundry Agent Service integration
- [ ] T055 [P] Update `docs/QUICKSTART.md` — replace any references to `FoundryModels` with `MicrosoftFoundry`, update endpoint examples to project endpoint format
- [ ] T056 [P] Update `AGENTS.md` and `.github/copilot-instructions.md` — replace `FoundryModels` references with `MicrosoftFoundry`, update AI integration patterns to reflect `AIProjectClient`
- [ ] T057 [P] Update `.specify/memory/constitution.md` Principle II — replace `FoundryModels` reference with `MicrosoftFoundry` in the bullet "AI service configuration MUST support Azure AI Foundry via `FoundryModels` settings"
- [ ] T058 Run `dotnet format genai-database-explorer-service/GenAIDBExplorer.slnx` and VS Code task `format-fix-whitespace-only` to ensure formatting compliance
- [ ] T059 Build the full solution and run ALL unit tests (`dotnet exec` for Core.Test, Console.Test, Api.Test) to confirm zero regressions
- [ ] T060 Run quickstart.md validation — follow the steps in `specs/006-adopt-foundry-project-endpoint/quickstart.md` to verify end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) completion — BLOCKS all user stories. Includes T007 investigation that shapes US5 design.
- **US1 (Phase 3)**: Depends on Foundational (Phase 2) — core factory migration
- **US2 (Phase 4)**: Depends on Setup (Phase 1) — can run in parallel with US1 after Phase 2
- **US3 (Phase 5)**: Depends on Foundational (Phase 2) — refines error messages from Phase 2c
- **US4 (Phase 6)**: Independent of application code changes — can run in parallel with US1-US3
- **US5 (Phase 7)**: Depends on US1 (Phase 3) completion + T007 investigation — needs `AIProjectClient` accessible from factory
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational — no other story dependencies
- **US2 (P2)**: Depends only on Setup — can start after Phase 1 (independent of US1 factory code)
- **US3 (P3)**: Depends on Foundational (Phase 2) — refines validation already implemented in Phase 2c
- **US4 (P4)**: No code dependencies — Bicep-only, can start immediately after Phase 1
- **US5 (P5)**: Depends on US1 + T007 investigation — needs `AIProjectClient` / `GetProjectClient()` from factory

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Models/settings before services
- Services before CLI/infrastructure
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- T002, T003 can run in parallel (independent file renames)
- T008-T014 can run in parallel (independent test methods in same file)
- T022, T023, T024, T025 can run in parallel (independent test methods)
- T031, T032, T033, T034 can run in parallel (independent test methods)
- T039, T040 can run in parallel (independent test methods)
- T046, T047, T048, T049 can run in parallel (independent test methods)
- T054, T055, T056, T057 can run in parallel (independent documentation files)
- US2, US3, and US4 can all run in parallel with US1 (after their prerequisites)

---

## Parallel Example: User Story 1

```bash
# Launch all tests for US1 together (write first, expect failures):
T022: Update ChatClientFactoryTests mock settings
T023: Add ProjectEndpoint_ShouldReturnIChatClient test
T024: Add ProjectEndpoint_ShouldReturnEmbeddingGenerator test
T025: Add LogsProjectEndpoint test

# Then implement (sequential — same file):
T026: Refactor ChatClientFactory — remove FoundryTokenScope, add AIProjectClient
T027: Update CreateChatClient() to use project client
T028: Update CreateEmbeddingGenerator() to use project client
T029: Extend IChatClientFactory with GetProjectClient()
T030: Build and run tests
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (NuGet upgrades + class renames)
2. Complete Phase 2: Foundational (settings binding + validation + legacy detection)
3. Complete Phase 3: User Story 1 (AIProjectClient factory migration)
4. **STOP and VALIDATE**: Run `enrich-model` with a Foundry project endpoint to verify chat + embeddings work
5. Deploy/demo if ready — all existing AI commands work through the project endpoint

### Incremental Delivery

1. Setup + Foundational → Foundation ready (solution compiles, validation works)
2. Add US1 → Chat/embedding via project endpoint → **MVP!**
3. Add US2 → CLI generates correct settings → New projects use v2.0.0 schema
4. Add US3 → Legacy users get migration guidance → Smooth upgrade path
5. Add US4 → Infrastructure creates projects → Automated deployment
6. Add US5 → Agent on Foundry → Full platform integration
7. Each story adds value without breaking previous stories

### Suggested MVP Scope

**US1 only** — once `ChatClientFactory` uses `AIProjectClient`, all existing commands (enrich-model, generate-vectors, export-model) work through the Foundry project endpoint. This is the highest-value change with immediate user impact.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Run `format-fix-whitespace-only` VS Code task after any C# file changes
- Run `dotnet exec <test-dll>` (not `dotnet test`) for unit tests on .NET 10 SDK
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
