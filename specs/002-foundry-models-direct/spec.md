# Feature Specification: Migrate to Microsoft Foundry Models Direct

**Feature Branch**: `002-foundry-models-direct`
**Created**: 2026-02-22
**Status**: Draft
**Input**: User description: "Complete move to use Microsoft Foundry Projects for providing AI models to this application by removing support for OpenAI hosted models, removing direct use of Azure OpenAI Service (only use Microsoft Foundry Project endpoints), renaming settings/code that reference OpenAI or Azure OpenAI, and leveraging Microsoft Foundry Models Direct for all AI model interactions."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configure Foundry Models Endpoint (Priority: P1)

As a developer setting up GenAI Database Explorer, I want to configure a single Microsoft Foundry endpoint and credentials in project settings so that all AI model interactions (chat completions, structured output, embeddings) route through one unified endpoint instead of configuring separate OpenAI or Azure OpenAI service details.

**Why this priority**: This is the foundational change. Without the new configuration model, no other AI functionality works. It replaces the current multi-provider (OpenAI vs. Azure OpenAI) settings with a single Foundry-centric configuration, dramatically simplifying setup.

**Independent Test**: Can be fully tested by creating a project with a `settings.json` that uses the new Foundry configuration section, initializing the project, and verifying that the application loads and validates the settings without errors.

**Acceptance Scenarios**:

1. **Given** a project with the new `FoundryModels` settings section containing a valid Foundry endpoint and deployment names, **When** the application loads the project configuration, **Then** the settings are parsed and validated successfully.
1. **Given** a project with a `FoundryModels` section missing the required endpoint URL, **When** the application loads the project configuration, **Then** a clear validation error is reported indicating the missing endpoint.
1. **Given** a project with a `FoundryModels` section missing a required deployment name for chat completion, **When** the application loads the project configuration, **Then** a clear validation error is reported indicating the missing deployment name.
1. **Given** a project with Entra ID authentication configured, **When** the application creates an AI client, **Then** it authenticates using `DefaultAzureCredential` against the Foundry endpoint.
1. **Given** a project with API key authentication configured, **When** the application creates an AI client, **Then** it authenticates using the provided API key against the Foundry endpoint.

---

### User Story 2 - AI Operations via Foundry Models Direct (Priority: P1)

As a user running model enrichment, data dictionary processing, or natural language queries, I want all AI-powered operations to work through Microsoft Foundry Models Direct so that I can use any model deployed in my Foundry resource (including non-OpenAI models) with a unified experience.

**Why this priority**: This ensures all existing AI functionality (enrich-model, data-dictionary, query-model) continues working after the migration. It is equally critical as Story 1 since the application is non-functional without it.

**Independent Test**: Can be tested by running `enrich-model`, `data-dictionary`, and `query-model` commands against a configured Foundry resource with deployed models and verifying the operations produce correct results.

**Acceptance Scenarios**:

1. **Given** a project configured to use Foundry Models Direct, **When** a user runs the enrich-model command, **Then** AI-generated descriptions are produced for database entities using the configured chat completion deployment.
1. **Given** a project configured to use Foundry Models Direct with a structured output deployment, **When** the application extracts entity lists using structured output, **Then** the structured JSON responses are correctly parsed.
1. **Given** a project configured to use Foundry Models Direct with an embedding deployment, **When** a user runs generate-vectors, **Then** vector embeddings are generated for semantic model entities.

---

### User Story 3 - Removal of OpenAI-Specific References (Priority: P2)

As a developer maintaining or contributing to GenAI Database Explorer, I want all code, settings, configuration classes, and documentation to use Foundry-centric naming (no references to "OpenAI" or "Azure OpenAI") so that the codebase accurately reflects the technology being used and avoids confusion.

**Why this priority**: While the application can technically function with renamed internals, clean naming is important for maintainability, contributor onboarding, and documentation accuracy. It is secondary to functional correctness.

**Independent Test**: Can be tested by searching the codebase for any remaining references to "OpenAI" or "AzureOpenAI" in settings classes, factory classes, configuration keys, and project settings files and confirming none remain (except in third-party package references or historical documentation).

**Acceptance Scenarios**:

1. **Given** the migrated codebase, **When** a developer searches for "OpenAIService" in settings class names or JSON configuration keys, **Then** no matches are found; the equivalent section is named `FoundryModels` (or similar Foundry-centric name).
1. **Given** the migrated codebase, **When** a developer searches for "AzureOpenAIDeploymentId" in settings classes, **Then** no matches are found; the equivalent property is named `DeploymentName` (or similar model-agnostic name).
1. **Given** the migrated codebase, **When** a developer searches for "AzureOpenAIEndpoint" in settings classes, **Then** no matches are found; the equivalent property is named `Endpoint` within the `FoundryModels` section.
1. **Given** the default project template (`DefaultProject/settings.json`) and sample project settings (`samples/AdventureWorksLT/settings.json`), **When** a developer inspects them, **Then** they use the new Foundry-centric configuration structure.

---

### User Story 4 - Existing Project Migration Path (Priority: P2)

As an existing user of GenAI Database Explorer who has projects with `OpenAIService` settings, I want clear guidance or error messages when loading old-format settings so that I know exactly what needs to change in my `settings.json` to migrate to the new Foundry configuration.

**Why this priority**: Users with existing projects need a smooth transition. Clear error messages prevent confusion and support self-service migration.

**Independent Test**: Can be tested by loading a project with old-format `OpenAIService` settings and verifying the application produces a helpful error message explaining the required migration steps.

**Acceptance Scenarios**:

1. **Given** a project with the old `OpenAIService` settings section, **When** the application loads the project configuration, **Then** a clear error message is displayed indicating that `OpenAIService` has been replaced by `FoundryModels` and describes the migration path.
1. **Given** the application documentation (README, quickstart), **When** a user references it, **Then** it reflects the new Foundry-centric configuration and does not reference OpenAI-specific settings.

---

### User Story 5 - Updated Init-Project Command (Priority: P3)

As a new user running `init-project`, I want the generated default `settings.json` to contain the new Foundry-centric configuration template so that I start with the correct structure from the beginning.

**Why this priority**: Only affects new project creation. Existing functionality takes precedence.

**Independent Test**: Can be tested by running `init-project` and verifying the generated `settings.json` contains the new `FoundryModels` section with appropriate placeholder values and comments.

**Acceptance Scenarios**:

1. **Given** a user running the `init-project` command, **When** the default settings file is generated, **Then** it contains a `FoundryModels` section with placeholder endpoint, authentication, and deployment name fields.
1. **Given** a user inspecting the generated settings file, **When** they read the comments, **Then** the comments clearly explain what each Foundry configuration field means and provide examples.

---

### Edge Cases

- What happens when the Foundry endpoint URL uses an unexpected format (e.g., not ending in `.services.ai.azure.com`, `.openai.azure.com`, or `.cognitiveservices.azure.com`)?  The system should accept all three patterns as valid Foundry Models endpoints and reject others with a clear validation error.
- What happens when a deployment name specified in settings does not exist in the Foundry resource? The system should surface the error from the Foundry service clearly, indicating the deployment was not found.
- What happens when the configured model does not support structured output but is specified for the structured output deployment? The system should surface the error from the Foundry service and log it clearly.
- What happens when the configured model does not support embeddings but is specified for the embedding deployment? The system should surface the error from the Foundry service and log it clearly.
- What happens when Entra ID authentication fails due to missing permissions? The system should surface the authentication error with actionable guidance (e.g., check RBAC role assignments on the Foundry resource).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST replace the `OpenAIService` settings section with a new `FoundryModels` settings section that uses Foundry-centric naming and configuration.
- **FR-002**: System MUST support a single Foundry endpoint URL that serves all model deployments (chat completion, structured output, embeddings) rather than separate per-provider endpoints.
- **FR-003**: System MUST support Microsoft Entra ID authentication (via `DefaultAzureCredential`) for connecting to the Foundry Models endpoint.
- **FR-004**: System MUST support API key authentication as an alternative for connecting to the Foundry Models endpoint.
- **FR-005**: System MUST allow specifying deployment names for each model purpose (chat completion, structured chat completion, embeddings) within the `FoundryModels` settings.
- **FR-006**: System MUST remove all support for direct OpenAI (non-Azure) hosted model connections, including the `ServiceType` toggle between "OpenAI" and "AzureOpenAI".
- **FR-007**: System MUST remove all OpenAI-specific setting properties (e.g., `OpenAIKey`, `AzureOpenAIKey`, `AzureOpenAIDeploymentId`, `AzureOpenAIEndpoint`) and replace them with Foundry-centric equivalents (e.g., `Endpoint`, `ApiKey`, `DeploymentName`).
- **FR-008**: System MUST rename all C# classes, interfaces, enums, and files that contain "OpenAI" or "AzureOpenAI" in their names to use Foundry-centric naming (e.g., `OpenAIServiceSettings` → `FoundryModelsSettings`, `AzureOpenAIAuthenticationType` → `AuthenticationType`).
- **FR-009**: System MUST continue to produce `IChatClient` and `IEmbeddingGenerator<string, Embedding<float>>` instances via `IChatClientFactory` for all AI operations, preserving the existing consumer interface.
- **FR-010**: System MUST validate the new `FoundryModels` settings section during project loading, including required endpoint URL, required deployment names, and valid authentication configuration.
- **FR-011**: System MUST provide a clear, actionable error message when an old-format `OpenAIService` settings section is detected, guiding the user to migrate to the new `FoundryModels` format.
- **FR-012**: System MUST update the default project template (`DefaultProject/settings.json`) to use the new `FoundryModels` configuration structure.
- **FR-013**: System MUST update the sample project settings (`samples/AdventureWorksLT/settings.json`) to use the new `FoundryModels` configuration structure.
- **FR-014**: System MUST update all unit tests to use the new settings structure and verify the renamed classes function correctly.
- **FR-015**: System MUST support optional `TenantId` configuration for Entra ID authentication when the Foundry resource is in a different tenant than the default credential chain.
- **FR-016**: System MUST accept Foundry endpoint URLs in all three valid formats: `https://<resource>.services.ai.azure.com`, `https://<resource>.openai.azure.com`, and `https://<resource>.cognitiveservices.azure.com`.

### Key Entities

- **FoundryModelsSettings**: Top-level configuration section replacing `OpenAIServiceSettings`. Contains default connection settings and per-purpose deployment configurations.
- **FoundryModelsDefaultSettings**: Connection and authentication settings for the Foundry Models endpoint (endpoint URL, authentication type, optional API key, optional tenant ID). Replaces `OpenAIServiceDefaultSettings`.
- **AuthenticationType (enum)**: Authentication method for the Foundry Models endpoint. Values: `EntraIdAuthentication`, `ApiKey`. Replaces `AzureOpenAIAuthenticationType`.
- **ChatCompletionDeploymentSettings**: Deployment name for chat completion models. Replaces `OpenAIServiceChatCompletionSettings`.
- **ChatCompletionStructuredDeploymentSettings**: Deployment name for structured output models. Replaces `OpenAIServiceChatCompletionStructuredSettings`.
- **EmbeddingDeploymentSettings**: Deployment name for embedding models. Replaces `OpenAIServiceEmbeddingSettings`.

## Clarifications

### Session 2026-02-22

- Q: What should the canonical JSON settings section key be (replacing `OpenAIService`)? → A: `FoundryModels` — matches Microsoft's official "Microsoft Foundry Models" product branding. This cascades to all C# class prefixes (e.g., `FoundryModelsSettings`, `FoundryModelsDefaultSettings`).
- Q: Which endpoint hostname patterns should validation accept? → A: All three — `*.services.ai.azure.com`, `*.openai.azure.com`, and `*.cognitiveservices.azure.com`. All are valid Microsoft Foundry Models endpoints.
- Q: Which SDK should `ChatClientFactory` use internally to connect to the Foundry Models endpoint? → A: Keep `Azure.AI.OpenAI` SDK internally. It is the most mature, best documented for Foundry, and already integrated. Renaming the wrapper classes/settings (already planned) removes user-visible "OpenAI" references while keeping a stable transport layer.

## Assumptions

- The `Azure.AI.OpenAI` and `Microsoft.Extensions.AI` NuGet packages remain the internal transport layer. No SDK replacement is needed — only wrapper class/settings naming changes are in scope. The `Azure.AI.OpenAI` SDK is the most mature and best documented for Foundry Models endpoints.
- The `IChatClientFactory` interface contract (methods `CreateChatClient()`, `CreateStructuredOutputChatClient()`, `CreateEmbeddingGenerator()`) remains unchanged. Only the implementation and settings model change.
- Users will manually update their existing `settings.json` files. No automated migration tool is required, but clear error messages and documentation must guide the process.
- The Foundry Models endpoint supports all model capabilities currently used by the application: chat completions, structured output (JSON schema), and text embeddings.
- The Bicep infrastructure templates (`infra/main.bicep`) may need updates to deploy Foundry resources instead of standalone Azure OpenAI resources, but infrastructure changes are out of scope for this specification and will be addressed separately.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All AI-powered commands (`enrich-model`, `data-dictionary`, `query-model`, `generate-vectors`) function correctly when configured against a Microsoft Foundry Models endpoint.
- **SC-002**: No C# source files contain class names, interface names, or enum names that include "OpenAI" or "AzureOpenAI" (excluding third-party package references).
- **SC-003**: No settings JSON files (`DefaultProject/settings.json`, `samples/*/settings.json`) contain an `OpenAIService` configuration section; all use `FoundryModels`.
- **SC-004**: Project settings validation produces a clear, actionable error within 1 second when an old-format `OpenAIService` section is detected.
- **SC-005**: All existing unit tests pass after migration, with updated assertions reflecting the new naming and configuration structure.
- **SC-006**: A new user can configure the application with a Foundry Models endpoint in under 5 minutes following the updated documentation.
- **SC-007**: The application correctly authenticates using both Entra ID and API key mechanisms against the Foundry Models endpoint.
