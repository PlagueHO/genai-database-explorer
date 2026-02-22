# Data Model: Migrate to Microsoft Foundry Models Direct

**Feature**: `002-foundry-models-direct` | **Date**: 2026-02-22

## Entity Diagram (Before → After)

```text
BEFORE:
┌──────────────────────────┐
│   ProjectSettings        │
│ ─────────────────────── │
│ OpenAIService            │──→ OpenAIServiceSettings
│ Database                 │        │
│ SemanticModel            │        ├── Default ──→ OpenAIServiceDefaultSettings
│ ...                      │        │     ServiceType: string
└──────────────────────────┘        │     AzureAuthenticationType: AzureOpenAIAuthenticationType
                                    │     OpenAIKey: string?
                                    │     AzureOpenAIKey: string?
                                    │     AzureOpenAIEndpoint: string?
                                    │     TenantId: string?
                                    │     AzureOpenAIAppId: string?
                                    │     FoundryProjectEndpoint: string?
                                    │
                                    ├── ChatCompletion ──→ OpenAIServiceChatCompletionSettings
                                    │     ModelId: string?
                                    │     AzureOpenAIDeploymentId: string?
                                    │
                                    ├── ChatCompletionStructured ──→ OpenAIServiceChatCompletionStructuredSettings
                                    │     ModelId: string?
                                    │     AzureOpenAIDeploymentId: string?
                                    │
                                    └── Embedding ──→ OpenAIServiceEmbeddingSettings
                                          ModelId: string?
                                          AzureOpenAIDeploymentId: string?

AFTER:
┌──────────────────────────┐
│   ProjectSettings        │
│ ─────────────────────── │
│ FoundryModels            │──→ FoundryModelsSettings
│ Database                 │        │
│ SemanticModel            │        ├── Default ──→ FoundryModelsDefaultSettings
│ ...                      │        │     AuthenticationType: AuthenticationType
└──────────────────────────┘        │     ApiKey: string?
                                    │     Endpoint: string?  [Required]
                                    │     TenantId: string?
                                    │     FoundryProjectEndpoint: string?
                                    │
                                    ├── ChatCompletion ──→ ChatCompletionDeploymentSettings
                                    │     DeploymentName: string?  [Required]
                                    │
                                    ├── ChatCompletionStructured ──→ ChatCompletionStructuredDeploymentSettings
                                    │     DeploymentName: string?  [Required]
                                    │
                                    └── Embedding ──→ EmbeddingDeploymentSettings
                                          DeploymentName: string?  [Required]
```

## Entity Definitions

### FoundryModelsSettings (replaces OpenAIServiceSettings)

| Field | Type | Required | Default | Validation | Notes |
|-------|------|----------|---------|------------|-------|
| `PropertyName` | `const string` | — | `"FoundryModels"` | — | JSON section key |
| `Default` | `FoundryModelsDefaultSettings` | Yes | `new()` | `[Required]` | Connection/auth settings |
| `ChatCompletion` | `ChatCompletionDeploymentSettings` | Yes | `new()` | `[Required]` | Chat completion model deployment |
| `ChatCompletionStructured` | `ChatCompletionStructuredDeploymentSettings` | Yes | `new()` | `[Required]` | Structured output model deployment |
| `Embedding` | `EmbeddingDeploymentSettings` | Yes | `new()` | `[Required]` | Embedding model deployment |

### FoundryModelsDefaultSettings (replaces OpenAIServiceDefaultSettings)

| Field | Type | Required | Default | Validation | Notes |
|-------|------|----------|---------|------------|-------|
| `PropertyName` | `const string` | — | `"Default"` | — | JSON sub-section key |
| `AuthenticationType` | `AuthenticationType` | Yes | `EntraIdAuthentication` | Enum | Auth method for Foundry endpoint |
| `ApiKey` | `string?` | Conditional | `null` | Required when `AuthenticationType` = `ApiKey` | API key for Foundry endpoint |
| `Endpoint` | `string?` | Yes | `null` | `[Required]`, `[Url]`, must be HTTPS with valid domain | Foundry Models endpoint URL |
| `TenantId` | `string?` | No | `null` | — | Optional: Azure tenant ID for multi-tenant Entra ID |
| `FoundryProjectEndpoint` | `string?` | No | `null` | `[Url]` | Optional: Foundry project URL for connection discovery |

### AuthenticationType (replaces AzureOpenAIAuthenticationType)

| Value | Description |
|-------|-------------|
| `ApiKey` | Traditional API key authentication |
| `EntraIdAuthentication` | Microsoft Entra ID via DefaultAzureCredential (default) |

### ChatCompletionDeploymentSettings (replaces OpenAIServiceChatCompletionSettings)

| Field | Type | Required | Default | Validation | Notes |
|-------|------|----------|---------|------------|-------|
| `PropertyName` | `const string` | — | `"ChatCompletion"` | — | JSON sub-section key |
| `DeploymentName` | `string?` | Yes | `null` | Validated in `ValidateFoundryModelsConfiguration()` | Deployment name in Foundry resource |

### ChatCompletionStructuredDeploymentSettings (replaces OpenAIServiceChatCompletionStructuredSettings)

| Field | Type | Required | Default | Validation | Notes |
|-------|------|----------|---------|------------|-------|
| `PropertyName` | `const string` | — | `"ChatCompletionStructured"` | — | JSON sub-section key |
| `DeploymentName` | `string?` | Yes | `null` | Validated in `ValidateFoundryModelsConfiguration()` | Must support structured output |

### EmbeddingDeploymentSettings (replaces OpenAIServiceEmbeddingSettings)

| Field | Type | Required | Default | Validation | Notes |
|-------|------|----------|---------|------------|-------|
| `PropertyName` | `const string` | — | `"Embedding"` | — | JSON sub-section key |
| `DeploymentName` | `string?` | Yes | `null` | Validated in `ValidateFoundryModelsConfiguration()` | Used for vector embeddings |

### IChatCompletionDeploymentSettings (replaces IOpenAIServiceChatCompletionSettings)

| Field | Type | Notes |
|-------|------|-------|
| `DeploymentName` | `string?` | Deployment name in Foundry resource |

### IEmbeddingDeploymentSettings (replaces IOpenAIServiceEmbeddingSettings)

| Field | Type | Notes |
|-------|------|-------|
| `DeploymentName` | `string?` | Deployment name in Foundry resource |

## Relationships

```text
ProjectSettings ──1:1──→ FoundryModelsSettings
  FoundryModelsSettings ──1:1──→ FoundryModelsDefaultSettings
  FoundryModelsSettings ──1:1──→ ChatCompletionDeploymentSettings
  FoundryModelsSettings ──1:1──→ ChatCompletionStructuredDeploymentSettings
  FoundryModelsSettings ──1:1──→ EmbeddingDeploymentSettings

ChatCompletionDeploymentSettings ──implements──→ IChatCompletionDeploymentSettings
ChatCompletionStructuredDeploymentSettings ──implements──→ IChatCompletionDeploymentSettings
EmbeddingDeploymentSettings ──implements──→ IEmbeddingDeploymentSettings
```

## State Transitions

N/A — these are configuration entities with no runtime state transitions.

## Validation Rules (ValidateFoundryModelsConfiguration)

1. **Endpoint required**: `FoundryModels.Default.Endpoint` must be non-null/non-empty
1. **Endpoint HTTPS**: Must use `https://` scheme
1. **Endpoint domain**: Hostname must end with `.services.ai.azure.com`, `.openai.azure.com`, or `.cognitiveservices.azure.com`
1. **Deployment names required**: All three (`ChatCompletion.DeploymentName`, `ChatCompletionStructured.DeploymentName`, `Embedding.DeploymentName`) must be non-null/non-whitespace
1. **API key conditional**: When `AuthenticationType` is `ApiKey`, `ApiKey` must be non-null/non-whitespace
1. **Old section detection**: If raw configuration contains `OpenAIService` key, throw migration error (FR-011)

## Properties Removed (no equivalent in new model)

| Old Class | Old Property | Reason |
|-----------|-------------|--------|
| `OpenAIServiceDefaultSettings` | `ServiceType` | No more OpenAI vs AzureOpenAI toggle — Foundry only |
| `OpenAIServiceDefaultSettings` | `OpenAIKey` | Direct OpenAI support removed |
| `OpenAIServiceDefaultSettings` | `AzureOpenAIAppId` | Not applicable to Foundry Models |
| `OpenAIServiceChatCompletionSettings` | `ModelId` | OpenAI-only; Foundry uses deployment names |
| `OpenAIServiceChatCompletionStructuredSettings` | `ModelId` | Same |
| `OpenAIServiceEmbeddingSettings` | `ModelId` | Same |
