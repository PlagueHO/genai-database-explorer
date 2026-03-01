# Implementation Plan: Adopt Microsoft Foundry Project Endpoint

**Branch**: `006-adopt-foundry-project-endpoint` | **Date**: 2026-03-01 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-adopt-foundry-project-endpoint/spec.md`

## Summary

Migrate the application from direct `OpenAIClient` with endpoint override to `AIProjectClient` from the `Azure.AI.Projects` SDK, connecting through the Microsoft Foundry project endpoint (`https://<resource>.services.ai.azure.com/api/projects/<project-name>`). This unifies all AI operations (chat completion, embeddings, agent hosting) under a single project-scoped connection. The configuration section is renamed from `FoundryModels` to `MicrosoftFoundry` (breaking change, `SettingsVersion` 2.0.0), legacy configuration is detected with actionable error messages, and the query-model agent is migrated from local `AsAIAgent()` orchestration to a Foundry-hosted agent via the Foundry Agent Service.

## Technical Context

**Language/Version**: .NET 10 with C# 14 (primary constructors, collection expressions, records, pattern matching)
**Primary Dependencies**:

- `Microsoft.Extensions.AI` — `IChatClient`, `IEmbeddingGenerator<string, Embedding<float>>`, `ChatRole`, `UsageDetails`
- `Microsoft.Agents.AI` / `Microsoft.Agents.AI.OpenAI` — `AIAgent`, `AIFunctionFactory`, `RunStreamingAsync` (current local agent)
- `Azure.AI.Projects` 1.1.0 → **upgrade to 1.2.0-beta.5+** — `AIProjectClient`, `Agents`, `OpenAI` sub-clients
- `Azure.AI.Projects.OpenAI` 1.0.0-beta.5 → **upgrade to matching version** — `GetProjectChatClient`, `GetProjectEmbeddingClient`, `GetProjectResponsesClientForAgent`
- `Azure.Identity` 1.18.0 — `DefaultAzureCredential`
- `OpenAI` (transitive via `Azure.AI.OpenAI` 2.1.0) — `OpenAI.Chat.ChatClient`
- `System.CommandLine` — CLI command handlers
- `Fluid` (Liquid templates) — prompt template rendering

**Storage**: JSON (`settings.json` per project, `semanticmodel.json`), multiple persistence strategies (LocalDisk, AzureBlob, CosmosDB) via `ISemanticModelRepository`
**Testing**: MSTest + FluentAssertions + Moq (unit), PowerShell Pester (integration)
**Target Platform**: Cross-platform .NET 10 (Windows, Linux, macOS)
**Project Type**: .NET solution with Console CLI + Core library + API + Aspire AppHost
**Performance Goals**: No degradation from current — AI call latency dominated by upstream model response times
**Constraints**: Settings schema is a breaking change (version 2.0.0); must provide clear legacy migration errors
**Scale/Scope**: ~15 source files changed, ~10 test files updated, 1 Bicep file updated, 2 settings templates updated

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Semantic Model Integrity — PASS

No changes to the semantic model format, extraction, or enrichment logic. The semantic model remains the central artifact. Only the AI client layer beneath the providers changes.

### Principle II: AI Integration via Microsoft.Extensions.AI — PASS

- `IChatClientFactory` interface is preserved unchanged
- All AI operations continue to flow through `IChatClient` and `IEmbeddingGenerator`
- Prompt templates remain in `.prompt` files under `Core/PromptTemplates/`
- Token usage tracking preserved via `UsageDetails`
- Agent orchestration uses Foundry Agent Service (evolution of the Agent Framework pattern)

### Principle III: Repository Pattern for Persistence — PASS

No changes to `ISemanticModelRepository` or persistence strategies. This feature only affects the AI client layer.

### Principle IV: Project-Based Workflow — PASS

- `settings.json` remains the project configuration driver
- Section renamed `FoundryModels` → `MicrosoftFoundry` with version bump
- `-p/--project` parameter unchanged
- Settings validation enhanced (project endpoint format, legacy detection)

### Principle V: Test-First Development — PASS

- Unit tests must be written/updated before implementation
- Existing `ChatClientFactoryTests.cs` will be updated for new `AIProjectClient` pattern
- New tests for endpoint validation, legacy detection, settings version check
- Agent migration tests for Foundry-hosted agent creation and invocation

### Principle VI: CLI-First Interface — PASS

- CLI options remain kebab-case (`--foundry-endpoint`, `--foundry-chat-deployment`, etc.)
- `init-project` updated to generate `MicrosoftFoundry` section
- Error messages enhanced to be actionable (legacy migration guidance)

### Principle VII: Dependency Injection & Configuration — PASS

- `IChatClientFactory` remains a singleton
- `AIProjectClient` will be a singleton (reuse HTTP connections)
- Configuration loaded via `IOptions<T>` pattern with renamed settings class
- Service registration in `HostBuilderExtensions.ConfigureHost()` updated

### Gate Result: **ALL PASS** — proceed to design

## Project Structure

### Documentation (this feature)

```text
specs/006-adopt-foundry-project-endpoint/
├── plan.md              # This file
├── research.md          # Phase 0 output — technology decisions
├── data-model.md        # Phase 1 output — settings and entity models
├── quickstart.md        # Phase 1 output — developer quickstart
├── contracts/           # Phase 1 output — (N/A, no new API endpoints)
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
genai-database-explorer-service/
├── src/
│   ├── GenAIDBExplorer.Console/
│   │   └── CommandHandlers/
│   │       └── InitProjectCommandHandler.cs       # Update JSON paths FoundryModels → MicrosoftFoundry
│   ├── GenAIDBExplorer.Core/
│   │   ├── ChatClients/
│   │   │   ├── ChatClientFactory.cs               # Major refactor: OpenAIClient → AIProjectClient
│   │   │   └── IChatClientFactory.cs              # Interface unchanged (add AIProjectClient accessor?)
│   │   ├── DefaultProject/
│   │   │   └── settings.json                      # Rename section, update endpoint, version 2.0.0
│   │   ├── Models/Project/
│   │   │   ├── MicrosoftFoundrySettings.cs        # Renamed from FoundryModelsSettings.cs
│   │   │   ├── MicrosoftFoundryDefaultSettings.cs # Renamed from FoundryModelsDefaultSettings.cs
│   │   │   ├── ProjectSettings.cs                 # Property rename: FoundryModels → MicrosoftFoundry
│   │   │   └── Project.cs                         # Validation updates, legacy detection
│   │   ├── SemanticModelQuery/
│   │   │   └── SemanticModelQueryService.cs       # Agent migration: AsAIAgent → Foundry-hosted agent
│   │   └── GenAIDBExplorer.Core.csproj            # NuGet upgrades
│   └── GenAIDBExplorer.Api/                       # Minimal changes (settings references only)
├── tests/
│   └── unit/
│       ├── GenAIDBExplorer.Core.Test/
│       │   ├── ChatClients/
│       │   │   └── ChatClientFactoryTests.cs      # Update for AIProjectClient pattern
│       │   └── Models/Project/
│       │       └── ProjectTests.cs                # Update validation tests, add legacy detection tests
│       └── GenAIDBExplorer.Console.Test/
│           └── CommandHandlers/
│               └── InitProjectCommandHandlerTests.cs  # Update settings path references
├── samples/
│   └── AdventureWorksLT/
│       └── settings.json                          # Update to MicrosoftFoundry section
└── infra/
    └── main.bicep                                 # Add project creation, output project endpoint
```

**Structure Decision**: Existing .NET solution structure preserved. No new projects or directories created. Changes span the Core library (settings models, factory, agent service), Console app (CLI handler), infrastructure (Bicep), and corresponding unit tests.

## Implementation Phases

### Phase 1: Configuration Rename & Validation (FR-002, FR-003, FR-004, FR-009, FR-010, FR-011, FR-012, FR-013, FR-016, FR-021)

**Goal**: Rename settings section, add project endpoint validation, implement legacy detection, update CLI.

**Changes**:

1. **Rename `FoundryModelsSettings.cs` → `MicrosoftFoundrySettings.cs`**
   - Class: `FoundryModelsSettings` → `MicrosoftFoundrySettings`
   - `PropertyName`: `"FoundryModels"` → `"MicrosoftFoundry"`
   - Remove `ChatCompletionStructured` sub-section (if present)

2. **Rename `FoundryModelsDefaultSettings.cs` → `MicrosoftFoundryDefaultSettings.cs`**
   - Class: `FoundryModelsDefaultSettings` → `MicrosoftFoundryDefaultSettings`

3. **Update `ProjectSettings.cs`**
   - Property: `FoundryModels` → `MicrosoftFoundry`
   - Type: `FoundryModelsSettings` → `MicrosoftFoundrySettings`

4. **Update `Project.cs`**
   - Bind `MicrosoftFoundry` section instead of `FoundryModels`
   - Add legacy detection: check for `FoundryModels` section → actionable error
   - Add legacy detection: check `SettingsVersion < 2.0.0` → actionable error
   - Update `ValidateFoundryModelsConfiguration()` → `ValidateMicrosoftFoundryConfiguration()`
   - Add project endpoint path validation (`/api/projects/{name}` required)
   - Preserve existing `OpenAIService` legacy detection

5. **Update `DefaultProject/settings.json`**
   - Rename `"FoundryModels"` → `"MicrosoftFoundry"`
   - Update endpoint placeholder: `"https://<resource>.services.ai.azure.com/api/projects/<project-name>"`
   - Set `"SettingsVersion": "2.0.0"`
   - Remove `ChatCompletionStructured` entry (already absent)

6. **Update `InitProjectCommandHandler.cs`**
   - JSON path references: `"FoundryModels"` → `"MicrosoftFoundry"`
   - `--foundry-endpoint` option description: mention project endpoint format
   - Version override: write `"SettingsVersion": "2.0.0"`

7. **Update `samples/AdventureWorksLT/settings.json`**
   - Same renames as DefaultProject template

**Tests**:

- `ProjectTests.cs`: Test legacy `FoundryModels` detection, version check, project endpoint validation (valid, missing path, resource-only, legacy OpenAI, trailing slash)
- `ChatClientFactoryTests.cs`: Update mock settings references
- `InitProjectCommandHandlerTests.cs`: Verify generated settings use `MicrosoftFoundry`

### Phase 2: AIProjectClient Factory Migration (FR-001, FR-005, FR-006, FR-007, FR-008, FR-017, FR-018)

**Goal**: Replace `OpenAIClient` with `AIProjectClient` in `ChatClientFactory`.

**Changes**:

1. **Upgrade NuGet packages in `GenAIDBExplorer.Core.csproj`**
   - `Azure.AI.Projects`: `1.1.0` → `1.2.0-beta.5` (or latest)
   - `Azure.AI.Projects.OpenAI`: `1.0.0-beta.5` → matching version

2. **Refactor `ChatClientFactory.cs`**
   - Remove `FoundryTokenScope` constant
   - Remove `CreateOpenAIClient()` private method
   - Add `AIProjectClient` creation:
     - Entra ID: `new AIProjectClient(endpoint, new DefaultAzureCredential(options))`
     - API Key: `new AIProjectClient(endpoint, new AzureKeyCredential(apiKey))`
   - `CreateChatClient()`: `projectClient.OpenAI.GetProjectChatClient(deploymentName).AsIChatClient()`
   - `CreateEmbeddingGenerator()`: `projectClient.OpenAI.GetProjectEmbeddingClient(deploymentName).AsIEmbeddingGenerator()`
   - Log the project endpoint at creation time (FR-017)
   - Cache the `AIProjectClient` instance (singleton per factory instance)

3. **Optionally extend `IChatClientFactory`**
   - Consider adding `AIProjectClient GetProjectClient()` for agent service access
   - Or create a separate `IFoundryProjectClientFactory` interface

**Tests**:

- `ChatClientFactoryTests.cs`: Verify `AIProjectClient` creation with both auth types, verify chat/embedding clients are returned, verify missing endpoint/deployment errors

### Phase 3: Foundry-Hosted Agent Migration (FR-019, FR-020)

**Goal**: Migrate `SemanticModelQueryService` from local `AsAIAgent()` to Foundry-hosted agent.

**Changes**:

1. **Update `SemanticModelQueryService.cs`**
   - Remove dependency on `OpenAI.Chat.ChatClient` unwrapping
   - Get `AIProjectClient` from factory (or new service)
   - Create Foundry-hosted agent via `projectClient.Agents.CreateAgentVersion()`
   - Define `PromptAgentDefinition` with model, instructions
   - Use `ProjectResponsesClient` for query execution
   - Preserve token tracking and guardrails (rounds, budget, timeout)
   - Preserve streaming via channel pattern (adapt from `RunStreamingAsync` to Foundry response streaming)
   - Handle Foundry agent errors gracefully (project doesn't support agents, deployment missing)

2. **Investigate tool integration**
   - Determine if Foundry Prompt Agent supports function tools that call back to the application
   - If not, use Option C from research (local AIAgent with Foundry-backed `IChatClient`)
   - Document the decision in research.md

**Tests**:

- `SemanticModelQueryServiceTests.cs`: Mock `AIProjectClient`, verify agent creation, verify query execution, verify error handling

### Phase 4: Infrastructure Updates (FR-014, FR-015)

**Goal**: Create Foundry project in Bicep, output project endpoint.

**Changes**:

1. **Update `infra/main.bicep`**
   - Define `defaultProjectName` variable (e.g., `'genaidbexplorer'`)
   - Pass `projects` array and `defaultProject` to foundry module
   - Add output `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` with project endpoint URL

2. **Verify `infra/cognitive-services/accounts/main.bicep`**
   - Confirm `projects` parameter accepts the project definition
   - Confirm the project module creates the project successfully

**Tests**:

- Bicep linting (`bicep build`)
- Manual deployment validation

## Key Technical Decisions

| # | Decision | Rationale | Reference |
|---|----------|-----------|-----------|
| D1 | Use `AIProjectClient` as sole AI client factory | Unifies all Foundry project operations under one client; SDK manages auth scopes | research.md R1 |
| D2 | Rename `FoundryModels` → `MicrosoftFoundry` with version 2.0.0 | Breaking schema change; name reflects broader platform scope | research.md R3, spec FR-002 |
| D3 | Validate project endpoint path (`/api/projects/{name}`) | Prevent silent failures from wrong endpoint types | research.md R4, spec FR-003 |
| D4 | Remove hardcoded `FoundryTokenScope` | SDK manages token scope; hardcoding was a maintenance risk | research.md R1, spec FR-005 |
| D5 | Drop `ChatCompletionStructured` sub-section | All recent models support structured output; redundant config | spec clarification, research.md R7 |
| D6 | Foundry-hosted agent for query-model | Leverages managed infrastructure, tracing, future capabilities | research.md R5, spec FR-019 |
| D7 | Create Bicep project via existing module | Module already supports projects; just needs to be invoked | research.md R6, spec FR-014 |

## Complexity Tracking

No constitution violations identified — no complexity justifications needed.
