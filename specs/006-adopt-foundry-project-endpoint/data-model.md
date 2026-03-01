# Data Model: Adopt Microsoft Foundry Project Endpoint

**Feature Branch**: `006-adopt-foundry-project-endpoint`
**Generated**: 2026-03-01

## Settings Entities

### MicrosoftFoundrySettings (renamed from FoundryModelsSettings)

**File**: `GenAIDBExplorer.Core/Models/Project/MicrosoftFoundrySettings.cs`
**JSON Section**: `"MicrosoftFoundry"`

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| `Default` | `MicrosoftFoundryDefaultSettings` | Yes | Must have valid Endpoint | Connection defaults |
| `ChatCompletion` | `ChatCompletionDeploymentSettings` | Yes | Must have DeploymentName | Chat deployment config |
| `Embedding` | `EmbeddingDeploymentSettings` | Yes | Must have DeploymentName | Embedding deployment config |

> **Removed**: `ChatCompletionStructured` sub-section (deprecated — all recent models support structured output)

### MicrosoftFoundryDefaultSettings (renamed from FoundryModelsDefaultSettings)

**File**: `GenAIDBExplorer.Core/Models/Project/MicrosoftFoundryDefaultSettings.cs`

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| `Endpoint` | `string?` | Yes | Must be valid HTTPS URL with `/api/projects/{name}` path | Foundry project endpoint |
| `AuthenticationType` | `AuthenticationType` | Yes | Enum: `EntraIdAuthentication`, `ApiKey` | Auth method |
| `ApiKey` | `string?` | Conditional | Required when `AuthenticationType == ApiKey` | API key credential |
| `TenantId` | `string?` | No | Valid GUID format if provided | For multi-tenant Entra ID |

**New Validation Rules**:

1. Endpoint must be an absolute HTTPS URI
2. Endpoint path must contain `/api/projects/` followed by at least one non-empty segment
3. Reject `*.openai.azure.com` endpoints with specific error ("Legacy Azure OpenAI endpoints are not supported. Use a Foundry project endpoint: `https://<resource>.services.ai.azure.com/api/projects/<project-name>`")
4. Reject `*.cognitiveservices.azure.com` endpoints with specific error
5. Accept trailing slash (`/api/projects/myproject/`) or no trailing slash

### ProjectSettings

**File**: `GenAIDBExplorer.Core/Models/Project/ProjectSettings.cs`

| Field | Type | Change | Notes |
|-------|------|--------|-------|
| `SettingsVersion` | `Version` | Default: `2.0.0` | Bumped from `1.0.0` |
| `Database` | `DatabaseSettings` | Unchanged | |
| `DataDictionary` | `DataDictionarySettings` | Unchanged | |
| `SemanticModel` | `SemanticModelSettings` | Unchanged | |
| `SemanticModelRepository` | `SemanticModelRepositorySettings` | Unchanged | |
| `MicrosoftFoundry` | `MicrosoftFoundrySettings` | **Renamed** from `FoundryModels` | Breaking change |
| `VectorIndex` | `VectorIndexSettings` | Unchanged | |
| `QueryModel` | `QueryModelSettings` | Unchanged | |

## State Transitions

### Settings Version State Machine

```
┌─────────────────┐     load settings      ┌──────────────────────┐
│  SettingsVersion │────────────────────────▶│  Version Check       │
│  < 2.0.0         │                         │  (Project.cs)        │
│  (legacy)        │                         └──────────┬───────────┘
└─────────────────┘                                     │
                                                        ▼
                                              ┌──────────────────────┐
                                              │  ERROR: "Settings    │
                                              │  version X is no     │
                                              │  longer supported.   │
                                              │  Version 2.0.0       │
                                              │  required."          │
                                              └──────────────────────┘

┌─────────────────┐     load settings      ┌──────────────────────┐
│  SettingsVersion │────────────────────────▶│  Version Check       │
│  >= 2.0.0        │                         │  (Project.cs)        │
│  (current)       │                         └──────────┬───────────┘
└─────────────────┘                                     │
                                                        ▼
                                              ┌──────────────────────┐
                                              │  Validate            │
                                              │  MicrosoftFoundry    │
                                              │  section             │
                                              └──────────┬───────────┘
                                                        │
                                                        ▼
                                              ┌──────────────────────┐
                                              │  OK: Proceed with    │
                                              │  AIProjectClient     │
                                              └──────────────────────┘
```

### Legacy Section Detection

```
┌──────────────────┐     load config      ┌─────────────────────────┐
│  settings.json   │─────────────────────▶│  Check for sections     │
│  (any version)   │                       └──────────┬──────────────┘
└──────────────────┘                                  │
                                     ┌────────────────┼────────────────┐
                                     ▼                ▼                ▼
                              ┌────────────┐   ┌────────────┐   ┌─────────────┐
                              │ OpenAI     │   │ Foundry    │   │ Microsoft   │
                              │ Service    │   │ Models     │   │ Foundry     │
                              │ (legacy v0)│   │ (legacy v1)│   │ (current)   │
                              └─────┬──────┘   └─────┬──────┘   └──────┬──────┘
                                    ▼                ▼                  ▼
                              ┌────────────┐   ┌────────────┐   ┌─────────────┐
                              │ ERROR:     │   │ ERROR:     │   │ Continue    │
                              │ Rename to  │   │ Rename to  │   │ validation  │
                              │ Microsoft  │   │ Microsoft  │   │             │
                              │ Foundry    │   │ Foundry    │   └─────────────┘
                              └────────────┘   └────────────┘
```

## Entity Relationships

```
ProjectSettings
├── MicrosoftFoundrySettings (1:1)
│   ├── MicrosoftFoundryDefaultSettings (1:1) — endpoint, auth
│   ├── ChatCompletionDeploymentSettings (1:1) — deployment name
│   └── EmbeddingDeploymentSettings (1:1) — deployment name
├── DatabaseSettings (1:1) — unchanged
├── DataDictionarySettings (1:1) — unchanged
├── SemanticModelSettings (1:1) — unchanged
├── SemanticModelRepositorySettings (1:1) — unchanged
├── VectorIndexSettings (1:1) — unchanged
└── QueryModelSettings (1:1) — agent name, instructions, guardrails
```

## AI Client Architecture (After Migration)

```
┌──────────────────────────────┐
│  IChatClientFactory          │
│  (singleton)                 │
├──────────────────────────────┤
│  CreateChatClient()          │──▶ projectClient.OpenAI.GetProjectChatClient() → .AsIChatClient()
│  CreateStructuredOutput...() │──▶ delegates to CreateChatClient()
│  CreateEmbeddingGenerator()  │──▶ projectClient.OpenAI.GetProjectEmbeddingClient() → .AsIEmbeddingGenerator()
│  GetProjectClient()          │──▶ returns cached AIProjectClient (for agent service access)
└──────────┬───────────────────┘
           │
           │  creates once
           ▼
┌──────────────────────────────┐
│  AIProjectClient             │
│  (cached per factory)        │
├──────────────────────────────┤
│  .OpenAI                     │──▶ OpenAI sub-client (chat, embeddings, responses)
│  .Agents                     │──▶ Agent service (create, invoke, delete)
│  .Connections                │──▶ (future use)
└──────────────────────────────┘
```

## settings.json Template (version 2.0.0)

```json
{
    "SettingsVersion": "2.0.0",
    "Database": { "..." },
    "DataDictionary": { "..." },
    "SemanticModel": { "..." },
    "SemanticModelRepository": { "..." },
    "MicrosoftFoundry": {
        "Default": {
            "AuthenticationType": "EntraIdAuthentication",
            "Endpoint": "https://<resource>.services.ai.azure.com/api/projects/<project-name>"
        },
        "ChatCompletion": {
            "DeploymentName": "<deployment-name>"
        },
        "Embedding": {
            "DeploymentName": "<deployment-name>"
        }
    },
    "VectorIndex": { "..." },
    "QueryModel": { "..." }
}
```
