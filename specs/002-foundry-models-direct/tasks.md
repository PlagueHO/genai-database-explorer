# Tasks: Migrate to Microsoft Foundry Models Direct

**Input**: Design documents from `/specs/002-foundry-models-direct/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/settings-schema.md, quickstart.md

**Tests**: Tests ARE included — the spec requires updating all existing unit tests (FR-014) and the constitution mandates test-first development.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Rename files and create foundational type skeletons. No logic changes yet.

- [X] T001 [P] Rename `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/AzureOpenAIAuthenticationType.cs` to `AuthenticationType.cs`, rename enum from `AzureOpenAIAuthenticationType` to `AuthenticationType`, keep values `ApiKey` and `EntraIdAuthentication`
- [X] T002 [P] Rename `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/IOpenAIServiceChatCompletionSettings.cs` to `IChatCompletionDeploymentSettings.cs`, rename interface to `IChatCompletionDeploymentSettings`, replace `ModelId` and `AzureOpenAIDeploymentId` with single `DeploymentName` property
- [X] T003 [P] Rename `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/IOpenAIServiceEmbeddingSettings.cs` to `IEmbeddingDeploymentSettings.cs`, rename interface to `IEmbeddingDeploymentSettings`, replace `ModelId` and `AzureOpenAIDeploymentId` with single `DeploymentName` property
- [X] T004 [P] Rename `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/OpenAIServiceChatCompletionSettings.cs` to `ChatCompletionDeploymentSettings.cs`, rename class to `ChatCompletionDeploymentSettings`, implement `IChatCompletionDeploymentSettings`, replace properties with `DeploymentName`
- [X] T005 [P] Rename `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/OpenAIServiceChatCompletionStructuredSettings.cs` to `ChatCompletionStructuredDeploymentSettings.cs`, rename class to `ChatCompletionStructuredDeploymentSettings`, implement `IChatCompletionDeploymentSettings`, replace properties with `DeploymentName`
- [X] T006 [P] Rename `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/OpenAIServiceEmbeddingSettings.cs` to `EmbeddingDeploymentSettings.cs`, rename class to `EmbeddingDeploymentSettings`, implement `IEmbeddingDeploymentSettings`, replace properties with `DeploymentName`
- [X] T007 Rename `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/OpenAIServiceDefaultSettings.cs` to `FoundryModelsDefaultSettings.cs`, rename class to `FoundryModelsDefaultSettings`, remove `ServiceType`/`OpenAIKey`/`AzureOpenAIAppId`/`RequiredOnPropertyValue` attributes, rename `AzureOpenAIEndpoint` → `Endpoint`, `AzureOpenAIKey` → `ApiKey`, `AzureAuthenticationType` → `AuthenticationType` (type `AuthenticationType`). Also preserve `TenantId` property.
- [X] T008 Rename `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/OpenAIServiceSettings.cs` to `FoundryModelsSettings.cs`, rename class to `FoundryModelsSettings`, update `PropertyName` to `"FoundryModels"`, update sub-setting types to new names (`FoundryModelsDefaultSettings`, `ChatCompletionDeploymentSettings`, `ChatCompletionStructuredDeploymentSettings`, `EmbeddingDeploymentSettings`)
- [X] T009 Run `dotnet format` via VS Code task `format-fix-whitespace-only` to fix formatting after Phase 1 renames

> **Note**: After Phase 1, `RequiredOnPropertyValueAttribute.cs` will have zero usages (its consumers `[RequiredOnPropertyValue(nameof(ServiceType), "OpenAI")]` on `OpenAIKey` and `[RequiredOnPropertyValue(nameof(ServiceType), "AzureOpenAI")]` on `AzureOpenAIEndpoint` are both removed in T007). The attribute class is retained for potential future use but is effectively dead code.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Update core infrastructure classes that all user stories depend on. Must complete before any story work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T010 Update `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/ProjectSettings.cs`: rename property `OpenAIService` to `FoundryModels`, change type from `OpenAIServiceSettings` to `FoundryModelsSettings`
- [X] T011 Update `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/Project.cs` `InitializeSettings()` method: change `OpenAIService = new OpenAIServiceSettings()` to `FoundryModels = new FoundryModelsSettings()`, update config binding from `OpenAIServiceSettings.PropertyName` to `FoundryModelsSettings.PropertyName` binding to `Settings.FoundryModels`
- [X] T012 Update `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/Project.cs` `ValidateSettings()` method: rename `ValidationContext(Settings.OpenAIService)` to `ValidationContext(Settings.FoundryModels)`, rename `ValidateOpenAIConfiguration()` call to `ValidateFoundryModelsConfiguration()`
- [X] T013 Rewrite `src/GenAIDBExplorer/GenAIDBExplorer.Core/Models/Project/Project.cs` validation method: rename `ValidateOpenAIConfiguration()` to `ValidateFoundryModelsConfiguration()`, add old-section detection (check `_configuration.GetSection("OpenAIService")` exists → throw `ValidationException` with migration message per FR-011), validate `Endpoint` is HTTPS with accepted domain patterns (`.services.ai.azure.com`, `.openai.azure.com`, `.cognitiveservices.azure.com`) per FR-016, validate all three `DeploymentName` properties are non-empty, validate `ApiKey` is present when `AuthenticationType` is `ApiKey`, remove all `ServiceType`/`ModelId`/OpenAI-specific validation branches
- [X] T014 Run `dotnet format` via VS Code task `format-fix-whitespace-only` to fix formatting after Phase 2 changes
- [X] T015 Run `dotnet build src/GenAIDBExplorer/GenAIDBExplorer.slnx` to verify compilation succeeds with all renames and modifications

**Checkpoint**: Core settings model and validation compile successfully. All type renames propagated.

---

## Phase 3: User Story 1 — Configure Foundry Models Endpoint (Priority: P1) 🎯 MVP

**Goal**: New `FoundryModels` settings section loads, validates, and authenticates correctly against Foundry endpoints.

**Independent Test**: Create a project with `FoundryModels` settings, load it, verify validation passes/fails correctly.

### Tests for User Story 1

> **NOTE: Write/update these tests FIRST, ensure they FAIL before implementation changes**

- [X] T016 [US1] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/Models/Project/ProjectSettingsIntegrationTests.cs`: replace all inline JSON `"OpenAIService"` sections with `"FoundryModels"` sections using new property names (`Endpoint`, `DeploymentName`, `AuthenticationType`), remove `ServiceType`/`ModelId` references, update all assertion strings from OpenAI names to Foundry names, add test for old-section detection (JSON with `OpenAIService` key → expect `ValidationException` with migration message), add tests for all three accepted endpoint URL patterns (`.services.ai.azure.com`, `.openai.azure.com`, `.cognitiveservices.azure.com`), add test for rejected endpoint URL pattern (e.g., `https://example.com/` → expect validation error per spec edge case 1)
- [X] T017 [P] [US1] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/ChatClients/ChatClientFactoryTests.cs`: replace all `OpenAIServiceSettings` with `FoundryModelsSettings`, `OpenAIServiceDefaultSettings` with `FoundryModelsDefaultSettings`, `AzureOpenAIAuthenticationType` with `AuthenticationType`, `AzureOpenAIDeploymentId` with `DeploymentName`, `AzureOpenAIEndpoint` with `Endpoint`, `AzureOpenAIKey` with `ApiKey`, `OpenAIService` property with `FoundryModels`, remove `ServiceType = "AzureOpenAI"` assignments, update all deployment setting types to new names. **Also add**: test case verifying `TenantId` is passed through to `DefaultAzureCredentialOptions.TenantId` when set, and ignored when null (FR-015 coverage)

### Implementation for User Story 1

- [X] T018 [US1] Update `src/GenAIDBExplorer/GenAIDBExplorer.Core/ChatClients/ChatClientFactory.cs`: change all `_project.Settings.OpenAIService` references to `_project.Settings.FoundryModels`, change `chatSettings.AzureOpenAIDeploymentId` to `chatSettings.DeploymentName`, change `embeddingSettings.AzureOpenAIDeploymentId` to `embeddingSettings.DeploymentName`, update `CreateAzureOpenAIClient` parameter type from `OpenAIServiceDefaultSettings` to `FoundryModelsDefaultSettings`, change `defaultSettings.AzureOpenAIEndpoint` to `defaultSettings.Endpoint`, change `defaultSettings.AzureAuthenticationType` to `defaultSettings.AuthenticationType`, change `AzureOpenAIAuthenticationType` enum references to `AuthenticationType`, change `defaultSettings.AzureOpenAIKey` to `defaultSettings.ApiKey`, update error messages from "AzureOpenAI" to "Foundry Models"
- [X] T019 [US1] Run `dotnet format` via VS Code task `format-fix-whitespace-only`
- [X] T020 [US1] Run all unit tests via `dotnet test src/GenAIDBExplorer/GenAIDBExplorer.slnx` to verify US1 tests pass

**Checkpoint**: Settings load with new `FoundryModels` section, validation works for all endpoint patterns, `ChatClientFactory` creates clients using new settings. Tests pass.

---

## Phase 4: User Story 2 — AI Operations via Foundry Models Direct (Priority: P1)

**Goal**: All AI-powered operations (enrich-model, data-dictionary, query-model, generate-vectors) work through the new Foundry settings path.

**Independent Test**: Run AI commands against a configured Foundry resource and verify operations produce correct results.

### Tests for User Story 2

> **NOTE: These tests already work via `IChatClientFactory` mock — they just need property renames**

- [X] T021 [P] [US2] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/SemanticProviders/SemanticDescriptionProviderTests.cs`: change `OpenAIService = new OpenAIServiceSettings()` to `FoundryModels = new FoundryModelsSettings()` in `ProjectSettings` construction
- [X] T022 [P] [US2] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/DataDictionary/DataDictionaryProviderTests.cs`: change `OpenAIService = new OpenAIServiceSettings()` to `FoundryModels = new FoundryModelsSettings()` in `ProjectSettings` construction
- [X] T023 [P] [US2] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/SemanticModelProviders/SemanticModelProvider.Tests.cs`: change `OpenAIService = new OpenAIServiceSettings()` to `FoundryModels = new FoundryModelsSettings()` in `ProjectSettings` construction
- [X] T024 [P] [US2] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/VectorEmbedding/Orchestration/VectorGenerationServiceTests.cs`: change `OpenAIService = new OpenAIServiceSettings()` to `FoundryModels = new FoundryModelsSettings()` in `ProjectSettings` construction
- [X] T025 [P] [US2] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/VectorEmbedding/E2E/InMemoryE2ETests.cs`: change `OpenAIService = new OpenAIServiceSettings()` to `FoundryModels = new FoundryModelsSettings()` in `ProjectSettings` construction
- [X] T026 [P] [US2] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/Data/DatabaseProviders/SqlConnectionProviderTests.cs`: change `OpenAIService = new OpenAIServiceSettings()` to `FoundryModels = new FoundryModelsSettings()` in `ProjectSettings` construction (2 occurrences)
- [X] T027 [US2] Run `dotnet format` via VS Code task `format-fix-whitespace-only`
- [X] T028 [US2] Run all unit tests via `dotnet test src/GenAIDBExplorer/GenAIDBExplorer.slnx` to verify US2 tests pass

**Checkpoint**: All AI operation provider tests pass with the new settings structure. `IChatClientFactory` consumers work unchanged.

---

## Phase 5: User Story 3 — Removal of OpenAI-Specific References (Priority: P2)

**Goal**: Zero OpenAI/AzureOpenAI references remain in settings classes, factory classes, configuration keys, or project settings files (excluding third-party package references).

**Independent Test**: Search codebase for "OpenAIService", "AzureOpenAIDeploymentId", "AzureOpenAIEndpoint" in settings classes and JSON — confirm no matches.

### Tests for User Story 3

- [X] T029 [P] [US3] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Console.Test/ExtractModelCommandHandlerTests.cs`: replace `OpenAIServiceSettings` with `FoundryModelsSettings`, `OpenAIService` property with `FoundryModels`, remove `Default.ServiceType = "AzureOpenAI"` assignments, update `Default.AzureOpenAIKey`/`Default.AzureOpenAIEndpoint` to new property names
- [X] T030 [P] [US3] Update `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Console.Test/ShowObjectCommandHandlerTests.cs`: change `OpenAIService = new OpenAIServiceSettings()` to `FoundryModels = new FoundryModelsSettings()` in `ProjectSettings` construction

### Implementation for User Story 3

- [X] T031 [US3] Verify codebase search: run `grep -r "OpenAIService\|AzureOpenAIDeploymentId\|AzureOpenAIEndpoint\|AzureOpenAIKey\|OpenAIServiceSettings\|AzureOpenAIAuthenticationType" --include="*.cs" src/GenAIDBExplorer/` and confirm zero matches in settings classes, factory classes, and project files (matches in third-party package references and `using` statements for `Azure.AI.OpenAI` SDK are expected and acceptable). **Also verify US3-AS4**: search `--include="*.json" src/ samples/` for `"OpenAIService"` to confirm default template and sample settings use new `FoundryModels` structure (if T035/T036 are not yet complete, note which JSON files still need updating)
- [X] T032 [US3] Run `dotnet format` via VS Code task `format-fix-whitespace-only`
- [X] T033 [US3] Run all unit tests via `dotnet test src/GenAIDBExplorer/GenAIDBExplorer.slnx` to verify all tests pass with no OpenAI references remaining

**Checkpoint**: SC-002 verified — no C# source files contain OpenAI-specific names except third-party package references.

---

## Phase 6: User Story 4 — Existing Project Migration Path (Priority: P2)

**Goal**: Old-format `OpenAIService` settings produce clear, actionable migration error.

**Independent Test**: Load a project with old `OpenAIService` JSON key → verify app throws `ValidationException` with migration guidance.

### Implementation for User Story 4

- [X] T034 [US4] Verify the old-section detection test added in T016 passes: run the specific test case that loads JSON with an `"OpenAIService"` section and expects a `ValidationException` with "FoundryModels" migration guidance. If T016 does not yet include this test case, add it to `ProjectSettingsIntegrationTests.cs` before verifying

**Checkpoint**: SC-004 verified — migration error detected in < 1 second with actionable message.

---

## Phase 7: User Story 5 — Updated Init-Project Command (Priority: P3)

**Goal**: Generated default `settings.json` uses new `FoundryModels` structure.

**Independent Test**: Run `init-project` → inspect generated `settings.json` → confirm `FoundryModels` section with correct placeholders.

### Implementation for User Story 5

- [X] T035 [US5] Rewrite `src/GenAIDBExplorer/GenAIDBExplorer.Core/DefaultProject/settings.json`: replace the `"OpenAIService"` section with `"FoundryModels"` section containing `Default.AuthenticationType`, `Default.Endpoint` (placeholder), `Default.ApiKey` (commented out), `Default.TenantId` (commented out), `ChatCompletion.DeploymentName` (placeholder), `ChatCompletionStructured.DeploymentName` (placeholder), `Embedding.DeploymentName` (placeholder), with descriptive JSONC comments per contracts/settings-schema.md
- [X] T036 [P] [US5] Rewrite `samples/AdventureWorksLT/settings.json`: replace the `"OpenAIService"` section with `"FoundryModels"` section matching the new schema, using sample values (e.g., `"Endpoint": "https://<Set your Foundry Models endpoint>.services.ai.azure.com/"`, `"DeploymentName": "<Set your deployment name>"`)

**Checkpoint**: SC-003 verified — no settings JSON files contain `OpenAIService`; all use `FoundryModels`.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, formatting, and full test suite verification

- [X] T037 Run `dotnet format` via VS Code task `format-fix-whitespace-only` to ensure all C# files are properly formatted
- [X] T038 Run full test suite: `dotnet test src/GenAIDBExplorer/GenAIDBExplorer.slnx` and confirm all tests pass (SC-005)
- [X] T039 Run build: `dotnet build src/GenAIDBExplorer/GenAIDBExplorer.slnx` and confirm zero errors and no new warnings
- [X] T040 Final verification: search for remaining `OpenAIService` references in `*.cs` and `*.json` files under `src/` and `samples/` — confirm none remain (excluding third-party `Azure.AI.OpenAI` SDK `using` statements and NuGet references)
- [X] T041 [US4] Update `docs/QUICKSTART.md`: replace all `OpenAIService` configuration examples with `FoundryModels` examples, update property names in instructions, add migration note for users upgrading from previous versions
- [X] T042 [P] [US4] Update `README.md`: replace any `OpenAIService` configuration references with `FoundryModels`, update setup instructions to reference Foundry Models endpoint
- [X] T043 [P] [US4] Update `specs/002-foundry-models-direct/quickstart.md`: verify migration guide content is accurate and complete against final implementation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion
- **User Story 2 (Phase 4)**: Depends on Phase 2 completion — can run in parallel with US1
- **User Story 3 (Phase 5)**: Depends on Phase 3 and Phase 4 completion (all renames must be done first)
- **User Story 4 (Phase 6)**: Depends on Phase 2 completion (migration detection in T013)
- **User Story 5 (Phase 7)**: Depends on Phase 1 completion only (just JSON file rewrites)
- **Polish (Phase 8)**: Depends on all previous phases

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational (Phase 2) — no cross-story dependencies
- **US2 (P1)**: Depends on Foundational (Phase 2) — can run in parallel with US1 since tests touch different files
- **US3 (P2)**: Depends on US1 + US2 — must verify after all renames are complete
- **US4 (P2)**: Depends on Foundational (Phase 2) — migration detection is in T013
- **US5 (P3)**: Independent of other stories — only modifies JSON template files

### Within Each User Story

- Tests written/updated FIRST — verify they compile (may fail until implementation)
- Implementation tasks in dependency order
- Format and test run at end of each story

### Parallel Opportunities

- T001, T002, T003, T004, T005, T006 can all run in parallel (different files, independent interfaces/classes)
- T017 can run in parallel with T016 (different test files)
- T021–T026 can all run in parallel (different test files, same property rename)
- T029, T030 can run in parallel (different test files)
- T035, T036 can run in parallel (different JSON files)
- US1 and US2 can run in parallel after Phase 2

---

## Parallel Example: Phase 1 Setup

```text
# These can all be done simultaneously (different files):
T001: Rename AzureOpenAIAuthenticationType.cs → AuthenticationType.cs
T002: Rename IOpenAIServiceChatCompletionSettings.cs → IChatCompletionDeploymentSettings.cs
T003: Rename IOpenAIServiceEmbeddingSettings.cs → IEmbeddingDeploymentSettings.cs
T004: Rename OpenAIServiceChatCompletionSettings.cs → ChatCompletionDeploymentSettings.cs
T005: Rename OpenAIServiceChatCompletionStructuredSettings.cs → ChatCompletionStructuredDeploymentSettings.cs
T006: Rename OpenAIServiceEmbeddingSettings.cs → EmbeddingDeploymentSettings.cs
```

## Parallel Example: Phase 4 Test Updates

```text
# These can all be done simultaneously (different test files):
T021: SemanticDescriptionProviderTests.cs
T022: DataDictionaryProviderTests.cs
T023: SemanticModelProvider.Tests.cs
T024: VectorGenerationServiceTests.cs
T025: InMemoryE2ETests.cs
T026: SqlConnectionProviderTests.cs
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup (file renames + type skeletons)
1. Complete Phase 2: Foundational (ProjectSettings, Project.cs binding + validation)
1. Complete Phase 3: US1 — settings loading + ChatClientFactory + tests
1. Complete Phase 4: US2 — provider test renames
1. **STOP and VALIDATE**: `dotnet test` — all AI operations work with new settings
1. Deploy/demo if ready

### Full Delivery

1. Complete Phase 5: US3 — verification of no OpenAI references
1. Complete Phase 6: US4 — migration error detection verified
1. Complete Phase 7: US5 — default/sample JSON files updated
1. Complete Phase 8: Polish — final format, build, test

### Parallel Team Strategy

With 2 developers after Phase 2:

- Developer A: US1 (Phase 3) → US3 (Phase 5) → US4 (Phase 6)
- Developer B: US2 (Phase 4) → US5 (Phase 7) → Polish (Phase 8)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- After every C# file change, run `format-fix-whitespace-only` task per constitution
- The `Azure.AI.OpenAI` SDK `using` statements in `ChatClientFactory.cs` are expected — the SDK is kept as internal transport
- `IChatClientFactory` interface is UNCHANGED — no consumer code needs modification
- `HostBuilderExtensions.cs` needs no changes — DI registration uses interface
