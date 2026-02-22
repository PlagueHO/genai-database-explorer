# Research: Migrate to Microsoft Foundry Models Direct

**Feature**: `002-foundry-models-direct` | **Date**: 2026-02-22

## 1. SDK Choice for Foundry Models Endpoint

- **Decision**: Keep `Azure.AI.OpenAI` SDK (`AzureOpenAIClient`) as internal transport layer.
- **Rationale**: Most mature, best documented for Foundry Models endpoints, already integrated. Microsoft recommends the OpenAI SDK for consuming Foundry Models. The `AzureOpenAIClient` natively supports all three endpoint hostname patterns.
- **Alternatives considered**:
  - `Azure.AI.Inference` (`ChatCompletionsClient`) — endpoint-agnostic but less mature, would require rewriting client creation code and new package dependency.
  - `Microsoft.Extensions.AI.OpenAI` bridge — thinnest wrapper but still delegates to OpenAI SDK internally, no real benefit.

## 2. Endpoint URL Validation Patterns

- **Decision**: Accept all three hostname patterns: `*.services.ai.azure.com`, `*.openai.azure.com`, `*.cognitiveservices.azure.com`.
- **Rationale**: All three are valid Microsoft Foundry Models endpoints. The `*.services.ai.azure.com` is the newest Foundry-native format, while the other two remain valid for backward compatibility.
- **Alternatives considered**:
  - Only two Foundry-era formats (excluding `.cognitiveservices.azure.com`) — rejected because existing deployments may still use this format.
  - No domain validation (any HTTPS URL) — rejected because validation catches configuration errors early.

## 3. Settings Section Naming

- **Decision**: `FoundryModels` as the canonical JSON settings section key.
- **Rationale**: Matches Microsoft's official "Microsoft Foundry Models" product branding. Cascades cleanly to C# class prefixes (`FoundryModelsSettings`, `FoundryModelsDefaultSettings`).
- **Alternatives considered**:
  - `AIModels` — too generic.
  - `FoundryAI` — doesn't reflect the "Models" product name.
  - `AzureAIFoundry` — too verbose for a JSON key.

## 4. Existing Codebase Analysis

### Files to RENAME (8 C# source files)

| Current File | New File | Current Class | New Class |
|---|---|---|---|
| `OpenAIServiceSettings.cs` | `FoundryModelsSettings.cs` | `OpenAIServiceSettings` | `FoundryModelsSettings` |
| `OpenAIServiceDefaultSettings.cs` | `FoundryModelsDefaultSettings.cs` | `OpenAIServiceDefaultSettings` | `FoundryModelsDefaultSettings` |
| `OpenAIServiceChatCompletionSettings.cs` | `ChatCompletionDeploymentSettings.cs` | `OpenAIServiceChatCompletionSettings` | `ChatCompletionDeploymentSettings` |
| `OpenAIServiceChatCompletionStructuredSettings.cs` | `ChatCompletionStructuredDeploymentSettings.cs` | `OpenAIServiceChatCompletionStructuredSettings` | `ChatCompletionStructuredDeploymentSettings` |
| `OpenAIServiceEmbeddingSettings.cs` | `EmbeddingDeploymentSettings.cs` | `OpenAIServiceEmbeddingSettings` | `EmbeddingDeploymentSettings` |
| `AzureOpenAIAuthenticationType.cs` | `AuthenticationType.cs` | `AzureOpenAIAuthenticationType` | `AuthenticationType` |
| `IOpenAIServiceChatCompletionSettings.cs` | `IChatCompletionDeploymentSettings.cs` | `IOpenAIServiceChatCompletionSettings` | `IChatCompletionDeploymentSettings` |
| `IOpenAIServiceEmbeddingSettings.cs` | `IEmbeddingDeploymentSettings.cs` | `IOpenAIServiceEmbeddingSettings` | `IEmbeddingDeploymentSettings` |

### Properties being REMOVED

| Class | Property | Reason |
|---|---|---|
| `OpenAIServiceDefaultSettings` | `ServiceType` | No more OpenAI vs AzureOpenAI toggle |
| `OpenAIServiceDefaultSettings` | `OpenAIKey` | Direct OpenAI support removed |
| `OpenAIServiceDefaultSettings` | `AzureOpenAIAppId` | Not used with Foundry Models |
| All deployment settings | `ModelId` | OpenAI-only field; Foundry uses deployment names |

### Properties being RENAMED

| Class | Old Property | New Property |
|---|---|---|
| Default settings | `AzureOpenAIEndpoint` | `Endpoint` |
| Default settings | `AzureOpenAIKey` | `ApiKey` |
| Default settings | `AzureAuthenticationType` | `AuthenticationType` |
| Deployment settings | `AzureOpenAIDeploymentId` | `DeploymentName` |

### Properties being KEPT unchanged

| Class | Property | Notes |
|---|---|---|
| Default settings | `TenantId` | Still needed for multi-tenant Entra ID |

### Files to MODIFY (non-rename, code changes)

| File | Changes |
|---|---|
| `ChatClientFactory.cs` | Update property paths (`OpenAIService` → `FoundryModels`, property renames) |
| `ProjectSettings.cs` | Rename `OpenAIService` property to `FoundryModels`, change type |
| `Project.cs` | Update initialization, binding (new `PropertyName`), validation (new method name + logic), add old-section detection (FR-011) |
| `DefaultProject/settings.json` | Rewrite `OpenAIService` section to `FoundryModels` |
| `samples/AdventureWorksLT/settings.json` | Same |
| `RequiredOnPropertyValueAttribute.cs` | No changes needed — conditional validation on `ServiceType` is being removed |

### Test Files (10 affected)

| File | Impact |
|---|---|
| `ChatClientFactoryTests.cs` | Heaviest — update all settings construction, property names, enum references |
| `ProjectSettingsIntegrationTests.cs` | Heaviest — update inline JSON, validation assertions |
| `ExtractModelCommandHandlerTests.cs` | Moderate — update settings construction |
| `ShowObjectCommandHandlerTests.cs` | Light — rename property only |
| `SemanticDescriptionProviderTests.cs` | Light — rename property only |
| `SemanticModelProvider.Tests.cs` | Light — rename property only |
| `SqlConnectionProviderTests.cs` | Light — rename property only |
| `DataDictionaryProviderTests.cs` | Light — rename property only |
| `VectorGenerationServiceTests.cs` | Light — rename property only |
| `InMemoryE2ETests.cs` | Light — rename property only |

### NuGet Packages (no changes)

| Package | Version | Status |
|---|---|---|
| `Azure.AI.OpenAI` | 2.1.0 | KEEP — internal transport |
| `Microsoft.Extensions.AI.OpenAI` | 10.3.0 | KEEP — extension methods |
| `Azure.AI.Projects.OpenAI` | 1.0.0-beta.5 | KEEP — future use |
| `Azure.AI.Projects` | 1.0.0-beta.5 | KEEP — future use |
| `Azure.Identity` | 1.17.1 | KEEP — Entra ID auth |

## 5. New Settings JSON Structure

```jsonc
"FoundryModels": {
    "Default": {
        "AuthenticationType": "EntraIdAuthentication", // EntraIdAuthentication (default) or ApiKey
        // "ApiKey": "<Set your API key>",             // Only required when AuthenticationType is "ApiKey"
        "Endpoint": "https://<resource>.services.ai.azure.com/" // Foundry Models endpoint
        // "TenantId": "<AzureTenantId>"               // Optional: for multi-tenant Entra ID
    },
    "ChatCompletion": {
        "DeploymentName": "<deployment-name>"           // e.g., "gpt-4-1"
    },
    "ChatCompletionStructured": {
        "DeploymentName": "<deployment-name>"           // Must support structured output
    },
    "Embedding": {
        "DeploymentName": "<deployment-name>"           // e.g., "text-embedding-ada-002"
    }
}
```

## 6. Migration Error Detection (FR-011)

When `Project.cs` loads settings, it should check whether the raw configuration contains an `OpenAIService` section key. If present, throw a `ValidationException` with a message like:

> "The 'OpenAIService' configuration section has been replaced by 'FoundryModels'. Please update your settings.json file. See documentation for the new configuration format."

This detection happens before normal validation so users get an actionable error immediately.

## 7. Endpoint Validation Logic (FR-016)

Replace the current validation in `ValidateOpenAIConfiguration()` (which checks `*.cognitiveservices.azure.com` and `*.openai.azure.com`) with a new `ValidateFoundryModelsConfiguration()` that:

1. Requires `Endpoint` to be a valid HTTPS URL
1. Validates the hostname ends with one of: `.services.ai.azure.com`, `.openai.azure.com`, `.cognitiveservices.azure.com`
1. Requires all three deployment names (`ChatCompletion.DeploymentName`, `ChatCompletionStructured.DeploymentName`, `Embedding.DeploymentName`)
1. Validates API key is present when `AuthenticationType` is `ApiKey`
