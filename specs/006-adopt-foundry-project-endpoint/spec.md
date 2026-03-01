# Feature Specification: Adopt Microsoft Foundry Project Endpoint

**Feature Branch**: `006-adopt-foundry-project-endpoint`
**Created**: 2025-07-24
**Status**: Draft
**Input**: User description: "Adopt the AIProjectClient Approach and ensure that the settings.json FoundryModels is now renamed to MicrosoftFoundry and the Endpoint setting always refers to the Project endpoint"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Connect to Microsoft Foundry via Project Endpoint (Priority: P1)

As a database analyst, I want the application to connect to Microsoft Foundry using the unified project endpoint so that all AI capabilities (chat completion, embeddings, agents, evaluations, tracing) are accessed through a single, consistent connection point rather than a legacy per-service endpoint.

Today the application connects directly to an Azure OpenAI endpoint (e.g., `*.openai.azure.com`). Microsoft Foundry (new) provides a unified project endpoint (`*.services.ai.azure.com/api/projects/<project-name>`) that acts as a single gateway for all AI services associated with a project. The application must adopt this project-centric connection model so that it can take advantage of Foundry project features such as agent hosting, evaluations, and tracing in future iterations.

**Why this priority**: This is the foundational change. Without connecting through the project endpoint, no other Foundry project capabilities are accessible. Every other story depends on this.

**Independent Test**: Can be fully tested by configuring a valid Foundry project endpoint in settings, running `extract-model` then `enrich-model`, and verifying that chat completions and embeddings are returned successfully through the project endpoint.

**Acceptance Scenarios**:

1. **Given** a settings file with a valid Foundry project endpoint and managed-identity authentication, **When** the user runs any AI-powered command (enrich-model, query-model, generate-vectors), **Then** the application connects through the project endpoint and returns results successfully.
1. **Given** a settings file with a valid Foundry project endpoint and API key authentication, **When** the user runs any AI-powered command, **Then** the application authenticates with the API key and returns results successfully.
1. **Given** a settings file with an endpoint that does not include a project path (e.g., just `*.services.ai.azure.com` without `/api/projects/<name>`), **When** the application loads the project, **Then** it reports a clear validation error indicating the endpoint must be a Foundry project endpoint.

---

### User Story 2 - Renamed Configuration Section (Priority: P2)

As a user configuring a new project, I want the settings section to be called `MicrosoftFoundry` instead of `FoundryModels` so that the configuration naming accurately reflects that the application connects to the broader Microsoft Foundry platform (not just "models") and is consistent with Microsoft's current branding.

The existing `FoundryModels` section name was chosen when the integration was limited to model inference. Since the application now connects through the Foundry project endpoint — which provides access to models, agents, evaluations, connections, and more — the name `MicrosoftFoundry` is more accurate and future-proof.

**Why this priority**: This is a configuration rename that must accompany the project endpoint change. New projects must use the new name, but this can be tested independently from the connection logic.

**Independent Test**: Can be tested by running `init-project` and verifying the generated settings file contains a `MicrosoftFoundry` section with the correct structure and an endpoint placeholder that indicates the project endpoint format.

**Acceptance Scenarios**:

1. **Given** a user runs `init-project` to create a new project, **When** the settings file is generated, **Then** it contains a `MicrosoftFoundry` section (not `FoundryModels`) with an endpoint placeholder showing the project endpoint format.
1. **Given** a settings file with the `MicrosoftFoundry` section and valid values, **When** the application loads the project, **Then** all configuration values are read correctly and the application operates normally.
1. **Given** the CLI `init-project` command, **When** the user provides `--foundry-endpoint`, **Then** the value is written into the `MicrosoftFoundry.Default.Endpoint` field in the generated settings file.

---

### User Story 3 - Legacy Configuration Detection and Migration Guidance (Priority: P3)

As a user with an existing project that uses the old `FoundryModels` configuration section, I want to receive a clear, actionable error message when I try to run the application so that I know exactly what to rename and how to update my settings file to the new `MicrosoftFoundry` format.

Users who created projects before this change will have a `FoundryModels` section in their `settings.json`. The application must detect this legacy section, refuse to proceed, and provide clear instructions for migrating to the new `MicrosoftFoundry` section and project endpoint format.

**Why this priority**: This protects existing users from silent failures. It is lower priority than the core functionality but is essential for a smooth upgrade experience.

**Independent Test**: Can be tested by loading a settings file that still contains a `FoundryModels` section and verifying the application emits a specific, helpful error message with migration instructions.

**Acceptance Scenarios**:

1. **Given** a settings file that contains a `FoundryModels` section but no `MicrosoftFoundry` section, **When** the application loads the project, **Then** it displays an error message instructing the user to rename the section to `MicrosoftFoundry` and update the endpoint to a Foundry project endpoint.
1. **Given** a settings file that contains both a `FoundryModels` section and a `MicrosoftFoundry` section, **When** the application loads the project, **Then** it displays an error indicating the ambiguity and instructs the user to remove the legacy `FoundryModels` section.
1. **Given** a settings file that contains only the `MicrosoftFoundry` section, **When** the application loads the project, **Then** no legacy warnings are emitted and the application proceeds normally.

---

### User Story 4 - Infrastructure Provisioning of Foundry Project (Priority: P4)

As a developer deploying the application's Azure infrastructure, I want the deployment templates to create a Microsoft Foundry project within the Foundry resource so that the deployment outputs include the project endpoint ready for use in `settings.json`.

Currently the infrastructure deploys a Foundry resource (AI Services account) but does not create a project within it. The Foundry project endpoint requires a project to exist. The infrastructure must create a default project and output its endpoint.

**Why this priority**: Infrastructure provisioning supports the end-to-end deployment story but is not required for users who create projects manually in the Azure portal.

**Independent Test**: Can be tested by deploying the infrastructure templates and verifying that the deployment outputs include a Foundry project endpoint in the correct format.

**Acceptance Scenarios**:

1. **Given** a user deploys the infrastructure, **When** the deployment completes, **Then** a Foundry project is created within the Foundry resource and the project endpoint is available as a deployment output.
1. **Given** the deployed infrastructure, **When** a user copies the project endpoint output into their `settings.json`, **Then** the application connects successfully using that endpoint.

---

### User Story 5 - Migrate Query Agent to Foundry-Hosted Agent (Priority: P5)

As a database analyst running natural language queries, I want the query-model agent to be hosted on the Foundry platform rather than orchestrated locally so that the agent benefits from Foundry's managed infrastructure, tracing, and future agent capabilities.

Today the query-model feature uses local agent orchestration via `AsAIAgent()` on a locally-constructed `OpenAI.Chat.ChatClient`. With the Foundry project endpoint in place, the agent should be created and managed through Foundry's agent hosting APIs, using the project connection for all AI interactions.

**Why this priority**: This builds on the project endpoint foundation (P1) and is a natural extension of the migration. It is lower priority than the core model inference migration since the local agent pattern already works.

**Independent Test**: Can be tested by running `query-model` with a natural language question and verifying that the agent executes through Foundry agent hosting, returning correct SQL and results.

**Acceptance Scenarios**:

1. **Given** a configured Foundry project endpoint with a chat completion deployment, **When** the user runs `query-model` with a natural language question, **Then** the agent is created and executed through Foundry agent hosting and returns a valid SQL query and results.
1. **Given** a Foundry-hosted agent session, **When** the agent requires multiple reasoning rounds (tool calls), **Then** all rounds execute successfully through the Foundry-hosted agent and token usage is tracked.
1. **Given** a Foundry project that does not support agent hosting, **When** the user runs `query-model`, **Then** the system reports a clear error indicating that agent hosting is required.

---

### Out of Scope

- **Foundry Evaluations**: Running evaluation pipelines through the Foundry project is future work.
- **Foundry Tracing/Telemetry**: Wiring up Foundry's distributed tracing capabilities is future work.
- **Foundry Connections Management**: Managing external data connections through the Foundry project API is future work.

### Edge Cases

- What happens when the project endpoint URL is valid HTTPS but points to a Foundry resource endpoint (no `/api/projects/` path segment)? The system must reject it with a clear error distinguishing resource endpoint from project endpoint.
- What happens when the project endpoint URL includes a trailing slash? The system must handle it gracefully (accept with or without trailing slash).
- What happens when a user provides a legacy `*.openai.azure.com` endpoint in the `MicrosoftFoundry` section? The system must reject it and explain that only Foundry project endpoints are accepted.
- What happens when the Foundry project does not have the required model deployments? The system must surface the upstream error clearly, indicating which deployment is missing.
- What happens when authentication fails against the project endpoint? The system must surface a clear authentication error with guidance (check managed identity role assignments or API key validity).
- What happens when a user has an existing project with a `FoundryModels` section containing a legacy `*.openai.azure.com` endpoint? The migration error must explain both the section rename and the endpoint format change.
- What happens when the Foundry project does not support agent hosting (e.g., the project lacks agent capabilities)? The system must surface a clear error when `query-model` is invoked, indicating the project must support agent hosting.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST connect to Microsoft Foundry exclusively through the project endpoint format (`https://<resource>.services.ai.azure.com/api/projects/<project-name>`).
- **FR-002**: System MUST read all AI service configuration from a settings section named `MicrosoftFoundry` (replacing the previous `FoundryModels` section).
- **FR-003**: System MUST validate that the configured `Endpoint` value contains a Foundry project path (i.e., includes `/api/projects/` followed by a project name). Endpoints without a project path MUST be rejected with a descriptive error.
- **FR-004**: System MUST validate that the configured `Endpoint` uses HTTPS.
- **FR-005**: System MUST support managed-identity authentication (via `DefaultAzureCredential`) against the Foundry project endpoint, delegating token scope management to the SDK rather than hardcoding any scope value.
- **FR-006**: System MUST support API key authentication against the Foundry project endpoint.
- **FR-007**: System MUST obtain chat completion capabilities from the Foundry project connection, not by connecting directly to a separate OpenAI endpoint.
- **FR-008**: System MUST obtain embedding generation capabilities from the Foundry project connection, not by connecting directly to a separate OpenAI endpoint.
- **FR-009**: System MUST detect the presence of a legacy `FoundryModels` section or a `SettingsVersion` below `2.0.0` in the settings file and report a clear, actionable error with migration instructions.
- **FR-010**: System MUST detect the presence of a legacy `OpenAIService` section in the settings file and report a clear, actionable error (preserving existing behavior).
- **FR-011**: The `init-project` CLI command MUST generate a settings file with a `MicrosoftFoundry` section containing an endpoint placeholder that demonstrates the project endpoint format.
- **FR-012**: The `init-project` CLI command's `--foundry-endpoint` option MUST write the provided value into the `MicrosoftFoundry.Default.Endpoint` field.
- **FR-013**: The `MicrosoftFoundry` settings section MUST contain sub-sections for `Default`, `ChatCompletion`, and `Embedding` only. The former `ChatCompletionStructured` sub-section is deprecated (all recent models support structured output) and MUST NOT be carried forward.
- **FR-014**: The infrastructure deployment templates MUST create a Foundry project within the Foundry resource.
- **FR-015**: The infrastructure deployment MUST output the Foundry project endpoint.
- **FR-016**: System MUST support the existing `--foundry-chat-deployment` and `--foundry-embedding-deployment` CLI options, writing their values into the `MicrosoftFoundry` section.
- **FR-017**: System MUST log the Foundry project endpoint being used (without sensitive details) at application startup for diagnostic purposes.
- **FR-018**: All existing AI-powered commands (enrich-model, query-model, generate-vectors, export-model) MUST continue to function correctly after the migration to the project endpoint.
- **FR-019**: The agent-powered query feature MUST be migrated from local agent orchestration (via `AsAIAgent()`) to a Foundry-hosted agent created and executed through the Foundry project endpoint.
- **FR-020**: The Foundry-hosted query agent MUST support multi-round tool-calling interactions (function tools for SQL execution) and track token usage across all rounds.
- **FR-021**: The `SettingsVersion` MUST be set to `2.0.0` in newly generated settings files. The application MUST detect settings files with a version below `2.0.0` and include the version mismatch in the legacy migration error message.

### Key Entities

- **MicrosoftFoundry Settings**: The configuration block in `settings.json` that holds the Foundry project endpoint, authentication settings, and model deployment names. Replaces the former `FoundryModels` block.
- **Foundry Project Endpoint**: A URL in the format `https://<resource>.services.ai.azure.com/api/projects/<project-name>` that serves as the single entry point for all AI operations within a Foundry project.
- **Foundry Project**: An Azure resource nested within a Foundry (AI Services) account that groups model deployments, connections, and capabilities under a single project endpoint.

## Clarifications

### Session 2026-03-01

- Q: Should the `MicrosoftFoundry` section retain the `ChatCompletionStructured` sub-section? → A: No — `ChatCompletionStructured` is deprecated because all recent models support structured output. Only `ChatCompletion` is needed; any remaining references to `ChatCompletionStructured` should be removed.
- Q: Should adopting additional Foundry project capabilities (agent hosting, evaluations, tracing, connections) be in scope? → A: Include agent hosting — migrate the query-model agent to a Foundry-hosted agent. Evaluations, tracing, and connections management are out of scope (future features).
- Q: Which token scope should the application use for authenticating against the Foundry project endpoint? → A: Delegate to the SDK — `AIProjectClient` handles scope selection automatically via `DefaultAzureCredential`. The application must not hardcode any token scope.
- Q: Should the `SettingsVersion` be bumped for this breaking schema change? → A: Yes — bump to `2.0.0`. The renamed section, removed sub-section, and new endpoint format constitute a major breaking change.

## Assumptions

- Users will have a Microsoft Foundry (new) resource with at least one project created (either manually or via the updated infrastructure templates).
- The Foundry project will have the required chat completion and embedding model deployments already provisioned.
- Managed-identity authentication requires the user or service identity to have appropriate role assignments on the Foundry resource/project.
- The `*.openai.azure.com` endpoint format is considered fully legacy and will not be supported in the `MicrosoftFoundry` section.
- The `*.cognitiveservices.azure.com` endpoint format is also considered legacy for this context.
- The sample project (`samples/AdventureWorksLT/settings.json`) will be updated to use the new `MicrosoftFoundry` section with a placeholder project endpoint.
- The Foundry SDK (`AIProjectClient`) manages authentication token scopes internally; the application must not hardcode token scopes such as `https://ai.azure.com/.default` or `https://cognitiveservices.azure.com/.default`.

## Dependencies

- Microsoft Foundry (new) must be generally available or in public preview with stable project endpoint support.
- The Foundry project endpoint must support OpenAI-compatible chat completion and embedding APIs.
- Azure infrastructure must support creating Foundry projects via Bicep/ARM templates.
- The Foundry project endpoint must support agent hosting APIs for creating and running agents with tool-calling capabilities.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All AI-powered commands (enrich-model, query-model, generate-vectors, export-model) complete successfully when configured with a valid Foundry project endpoint.
- **SC-002**: Users who run `init-project` receive a settings file with the `MicrosoftFoundry` section and a project endpoint placeholder — 100% of new projects use the new naming.
- **SC-003**: Users with legacy `FoundryModels` configuration receive a clear error message within 2 seconds of attempting to load the project, with specific instructions on how to migrate.
- **SC-004**: The infrastructure deployment creates a Foundry project and outputs a usable project endpoint without manual Azure portal steps.
- **SC-005**: No existing test scenarios regress — all unit and integration tests pass after the migration.
- **SC-006**: The endpoint validation correctly rejects non-project endpoints (resource-only endpoints, legacy OpenAI endpoints) and provides a specific, actionable error message for each case.
- **SC-007**: The `query-model` command executes using a Foundry-hosted agent and returns correct results for natural language queries against the semantic model.
