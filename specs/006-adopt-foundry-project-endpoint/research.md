# Research: Adopt Microsoft Foundry Project Endpoint

**Feature Branch**: `006-adopt-foundry-project-endpoint`
**Generated**: 2026-03-01
**Status**: Complete

## R1: AIProjectClient SDK Pattern for .NET

### Decision

Use `Azure.AI.Projects` package with `AIProjectClient` as the central entry point for all Foundry project interactions. The client is initialized with the project endpoint URL and `DefaultAzureCredential`.

### Rationale

- `AIProjectClient` is the official SDK entry point for Microsoft Foundry (new) projects
- It provides `.OpenAI` property to create project-scoped OpenAI clients (chat, embeddings, responses)
- It provides `.Agents` property for Foundry-hosted agent CRUD operations
- It handles token scope management internally — no hardcoded `https://ai.azure.com/.default` needed
- The project endpoint format `https://<resource>.services.ai.azure.com/api/projects/<project-name>` is the canonical URL

### Alternatives Considered

1. **Continue using raw `OpenAIClient` with endpoint override**: Rejected because this bypasses the Foundry project layer, preventing access to agent hosting, connections, evaluations, and tracing.
2. **Use `Azure.AI.OpenAI` `AzureOpenAIClient` directly**: Rejected because this targets the old Azure OpenAI endpoint and does not support Foundry project features.

### Code Pattern (from Microsoft docs)

```csharp
// Initialization
AIProjectClient projectClient = new(
    endpoint: new Uri("https://<resource>.services.ai.azure.com/api/projects/<project>"),
    tokenProvider: new DefaultAzureCredential());

// Chat completion via project-scoped OpenAI client
var chatClient = projectClient.OpenAI.GetProjectChatClient("<deployment-name>");

// Embeddings via project-scoped OpenAI client
var embeddingClient = projectClient.OpenAI.GetProjectEmbeddingClient("<deployment-name>");

// Agent creation via Foundry Agent Service
var agentVersion = projectClient.Agents.CreateAgentVersion(
    agentName: "myAgent",
    options: new(new PromptAgentDefinition(model: "<deployment>")
    {
        Instructions = "...",
        Tools = { ... }
    }));

// Agent execution via project responses client
var responseClient = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentVersion.Name);
var response = responseClient.CreateResponse("user query");
```

### NuGet Package Requirements

- **`Azure.AI.Projects`**: Upgrade from `1.1.0` to `1.2.0-beta.5` or latest GA (for Foundry new support)
- **`Azure.AI.Projects.OpenAI`**: Upgrade from `1.0.0-beta.5` to matching version (for `GetProjectChatClient`, `GetProjectResponsesClientForAgent`)
- **`Azure.Identity`**: Keep `1.18.0` (already compatible)

---

## R2: ChatClientFactory Refactoring Strategy

### Decision

Replace the current `OpenAIClient`-based factory with an `AIProjectClient`-based factory that creates project-scoped OpenAI clients.

### Rationale

The current `ChatClientFactory` creates a raw `OpenAIClient` with `BearerTokenPolicy` and hardcoded `FoundryTokenScope = "https://ai.azure.com/.default"`. This pattern:

1. Bypasses the Foundry project layer
2. Hardcodes a token scope that the SDK should manage
3. Cannot access Foundry agent hosting, connections, or other project features

The refactored factory will:

1. Create `AIProjectClient` from the project endpoint in settings
2. Use `projectClient.OpenAI.GetProjectChatClient()` → `.AsIChatClient()` for chat
3. Use `projectClient.OpenAI.GetProjectEmbeddingClient()` → `.AsIEmbeddingGenerator()` for embeddings
4. Expose the `AIProjectClient` for agent operations (via a new interface method or separate service)

### Alternatives Considered

1. **Maintain two clients (OpenAIClient + AIProjectClient)**: Rejected — unnecessary complexity; all OpenAI operations can go through the project client.
2. **Create AIProjectClient lazily per call**: Rejected — the client should be a singleton per project to reuse HTTP connections.

### Breaking Changes

- `FoundryTokenScope` constant removed (SDK manages scope)
- `CreateOpenAIClient()` private method removed (replaced by `AIProjectClient`)
- `IChatClientFactory` interface unchanged (but implementation completely rewritten)
- New method or service needed to expose `AIProjectClient` for agent operations

---

## R3: Configuration Rename Strategy (`FoundryModels` → `MicrosoftFoundry`)

### Decision

Rename the settings section from `FoundryModels` to `MicrosoftFoundry` with `SettingsVersion` bump to `2.0.0`.

### Rationale

- "FoundryModels" implies the section is only about model inference
- "MicrosoftFoundry" accurately reflects the broader platform (models, agents, evaluations, connections)
- Major version bump (`2.0.0`) signals a breaking schema change
- Legacy detection catches `FoundryModels` section OR `SettingsVersion < 2.0.0`

### Files Requiring Changes

| File | Change |
|------|--------|
| `FoundryModelsSettings.cs` | Rename class to `MicrosoftFoundrySettings`, change `PropertyName` to `"MicrosoftFoundry"` |
| `FoundryModelsDefaultSettings.cs` | Rename class to `MicrosoftFoundryDefaultSettings` |
| `ProjectSettings.cs` | Rename property `FoundryModels` → `MicrosoftFoundry`, change type |
| `Project.cs` | Update section binding, add legacy `FoundryModels` detection, update validation |
| `ChatClientFactory.cs` | Update property references from `.FoundryModels` to `.MicrosoftFoundry` |
| `DefaultProject/settings.json` | Rename section, update endpoint placeholder, set version `2.0.0` |
| `InitProjectCommandHandler.cs` | Update JSON path references from `"FoundryModels"` to `"MicrosoftFoundry"` |
| `samples/AdventureWorksLT/settings.json` | Update to new schema |
| `ChatClientFactoryTests.cs` | Update mock project settings references |
| All test files referencing `FoundryModels` | Update to new property names |

### Legacy Detection Logic

```csharp
// In Project.ValidateSettings():
if (configuration.GetSection("FoundryModels").Exists())
{
    // Error: "The 'FoundryModels' configuration section has been renamed to 'MicrosoftFoundry'.
    //         Please rename it in your settings.json. See migration guide at [URL]."
}

if (settings.SettingsVersion < new Version(2, 0))
{
    // Error: "Settings version {version} is no longer supported. Version 2.0.0 is required.
    //         The 'FoundryModels' section has been renamed to 'MicrosoftFoundry' and
    //         the endpoint must be a Foundry project endpoint."
}
```

---

## R4: Project Endpoint Validation

### Decision

Validate that the endpoint contains `/api/projects/` followed by a project name segment.

### Rationale

The current validation only checks for valid HTTPS URL format. The new validation must enforce the Foundry project endpoint format to prevent users from accidentally using:

- Resource-level endpoints (`*.services.ai.azure.com/` without a project)
- Legacy Azure OpenAI endpoints (`*.openai.azure.com`)
- Legacy Cognitive Services endpoints (`*.cognitiveservices.azure.com`)

### Validation Rules

1. Must be a valid, absolute HTTPS URI
2. Must contain path segments matching `/api/projects/{non-empty-name}`
3. Trailing slash handling: Accept both `https://...services.ai.azure.com/api/projects/myproject` and `https://...services.ai.azure.com/api/projects/myproject/`
4. Error messages should be specific to the case (resource-only endpoint vs. legacy OpenAI endpoint vs. missing project path)

### Alternatives Considered

1. **Regex-only validation**: Rejected — URI parsing is more robust for edge cases (encoded characters, query strings).
2. **No validation (let SDK fail)**: Rejected — SDK errors are opaque; user-facing validation provides better DX.

---

## R5: Foundry-Hosted Agent Migration

### Decision

Migrate `SemanticModelQueryService` from local agent orchestration (`AsAIAgent()`) to Foundry-hosted agent via the `AIProjectClient.Agents` and `ProjectResponsesClient` APIs.

### Rationale

- Foundry-hosted agents provide managed infrastructure, tracing, and logging
- The Foundry Agent Service supports Prompt Agents with function tools
- The project endpoint enables full agent lifecycle management (create, invoke, delete)
- This aligns with the broader Foundry adoption strategy

### Current Pattern (local orchestration)

```csharp
var chatClient = _chatClientFactory.CreateChatClient();
var openAiChatClient = chatClient.GetService<OpenAI.Chat.ChatClient>();
var agent = OpenAIChatClientExtensions.AsAIAgent(openAiChatClient, ...tools...);
var session = await agent.CreateSessionAsync();
await foreach (var update in agent.RunStreamingAsync(question, session)) { ... }
```

### Target Pattern (Foundry-hosted agent)

Two approaches are available:

**Option A: Prompt Agent via Foundry Agent Service (Recommended)**

```csharp
// One-time agent creation (cached)
var projectClient = /* from factory */;
var agentVersion = projectClient.Agents.CreateAgentVersion(
    agentName: settings.AgentName,
    options: new(new PromptAgentDefinition(model: chatDeploymentName)
    {
        Instructions = instructions,
        // Note: Function tools are NOT directly supported on Prompt Agents.
        // The search tools need to be converted to Foundry-compatible tools
        // or the agent pattern needs to use a different tool mechanism.
    }));

// Per-query execution
var responseClient = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentVersion.Name);
var response = responseClient.CreateResponse(question);
```

**Option B: PersistentAgent via Agent Framework + Foundry**

```csharp
var projectClient = /* from factory */;
var agentsClient = projectClient.GetPersistentAgentsClient();
var agent = await agentsClient.Administration.CreateAgentAsync(
    model: chatDeploymentName,
    name: settings.AgentName,
    instructions: instructions);
var thread = agentsClient.Threads.CreateThread();
var message = agentsClient.Messages.CreateMessage(thread.Id, MessageRole.User, question);
var run = agentsClient.Runs.CreateRun(thread.Id, agent.Id);
```

**Option C: Keep local AIAgent with Foundry-backed IChatClient (Pragmatic)**

```csharp
// Use AIProjectClient for model access but keep local agent orchestration
var chatClient = projectClient.OpenAI.GetProjectChatClient(deploymentName);
var agent = chatClient.AsAIAgent(...tools...);
// Same local streaming loop, but model calls go through project endpoint
```

### Decision: Option A for clean Foundry integration, with Option C as fallback

Option A (Prompt Agent) is preferred because it fully leverages Foundry's managed agent infrastructure. However, function tools for semantic model search need careful design — the search tools are local to the application (they query the vector index directly), so they cannot be Foundry-hosted tools.

**Recommended approach**: Use Option A (Prompt Agent) if the Foundry Responses API supports providing tool results back to the service. If not feasible, fall back to Option C (local AIAgent with Foundry-backed model).

### Investigation Required During Implementation

- Can `ProjectResponsesClient` handle multi-turn tool-calling where the application provides tool results?
- Can the existing `AIFunctionFactory`-based tools work with the Foundry Responses API?
- What is the latency overhead of Foundry-hosted agents vs. local orchestration?

---

## R6: Infrastructure — Bicep Project Creation

### Decision

Pass a project definition to the existing `cognitiveService_projects` module in `infra/cognitive-services/accounts/main.bicep` from `infra/main.bicep`, and output the project endpoint.

### Rationale

The existing Bicep module already supports project creation:
- `projects projectType[]?` parameter exists
- `defaultProject string?` parameter exists
- `cognitiveService_projects` module loop iterates `projects ?? []`

The main.bicep currently passes `allowProjectManagement: true` but does NOT pass any `projects`. The fix is:

1. Define a default project in main.bicep
2. Pass it to the module
3. Output the constructed project endpoint URL

### Implementation

```bicep
// In main.bicep
var defaultProjectName = 'genaidbexplorer'

// Pass to foundry module
projects: [
  {
    name: defaultProjectName
    // Other project properties as needed
  }
]
defaultProject: defaultProjectName

// Output
output AZURE_AI_FOUNDRY_PROJECT_ENDPOINT string = 'https://${foundryService.outputs.name}.services.ai.azure.com/api/projects/${defaultProjectName}'
```

### Alternatives Considered

1. **Create project via separate Az CLI/PowerShell script**: Rejected — Bicep support exists and is more declarative.
2. **Require manual project creation**: Rejected — the spec requires automated provisioning (FR-014, FR-015).

---

## R7: Drop `ChatCompletionStructured` Sub-Section

### Decision

Remove all references to `ChatCompletionStructured` from settings, code, and tests. The `CreateStructuredOutputChatClient()` method delegates directly to `CreateChatClient()`.

### Rationale

Per clarification: "ChatCompletionStructured is deprecated because all recent models support structured output. Only ChatCompletion is needed."

The current `CreateStructuredOutputChatClient()` already just delegates to `CreateChatClient()`. The `ChatCompletionStructuredDeploymentSettings` type (if it exists) and any settings references should be removed.

### Files to Check/Update

- `FoundryModelsSettings.cs` / `MicrosoftFoundrySettings.cs`: Ensure no `ChatCompletionStructured` property
- `DefaultProject/settings.json`: Ensure no `ChatCompletionStructured` entry
- `IChatClientFactory.cs`: Keep `CreateStructuredOutputChatClient()` (it delegates to `CreateChatClient()`)
- `ChatClientFactory.cs`: Confirm delegation pattern is preserved

### Alternatives Considered

- **Remove `CreateStructuredOutputChatClient()` entirely**: Rejected — too many callers; the method serves as a semantic alias even if the implementation is identical.

---

## R8: API Key Authentication with AIProjectClient

### Decision

Support API key authentication via `AIProjectClient` constructor overload.

### Rationale

The current code supports both Entra ID and API key authentication. `AIProjectClient` supports `AzureKeyCredential` for API key auth:

```csharp
// Entra ID
new AIProjectClient(endpoint, new DefaultAzureCredential());

// API Key
new AIProjectClient(endpoint, new AzureKeyCredential(apiKey));
```

Both patterns must be preserved in the refactored factory.

### Alternatives Considered

- **Drop API key support**: Rejected — some development/testing scenarios require API keys.
