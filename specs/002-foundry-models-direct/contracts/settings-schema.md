# Contracts: Foundry Models Settings Schema

**Feature**: `002-foundry-models-direct` | **Date**: 2026-02-22

This feature does not expose REST/API endpoints. The "contract" is the JSON settings schema that users author in `settings.json`. This document defines the new schema.

## Settings JSON Contract: `FoundryModels` Section

```jsonc
{
    "FoundryModels": {
        "Default": {
            // REQUIRED: Authentication method for the Foundry Models endpoint.
            // "EntraIdAuthentication" — default, uses DefaultAzureCredential (managed identity, az login, etc.)
            // "ApiKey" — traditional API key authentication
            "AuthenticationType": "EntraIdAuthentication",

            // CONDITIONAL: API key for the Foundry Models endpoint.
            // Required when AuthenticationType is "ApiKey". Ignored otherwise.
            // "ApiKey": "<your-api-key>",

            // REQUIRED: Microsoft Foundry Models endpoint URL.
            // Must be HTTPS. Accepted domain patterns:
            //   - https://<resource>.services.ai.azure.com/
            //   - https://<resource>.openai.azure.com/
            //   - https://<resource>.cognitiveservices.azure.com/
            "Endpoint": "https://<resource>.services.ai.azure.com/",

            // OPTIONAL: Azure tenant ID for DefaultAzureCredential.
            // Use when the Foundry resource is in a different tenant than the default credential chain.
            // "TenantId": "<azure-tenant-id>"
        },
        "ChatCompletion": {
            // REQUIRED: Deployment name for chat completion models in the Foundry resource.
            // Used for AI-generated descriptions, natural language queries, and data dictionary processing.
            "DeploymentName": "<deployment-name>"
        },
        // REQUIRED: Separate deployment for structured chat completion.
        // Must be a model that supports structured output (JSON schema mode).
        "ChatCompletionStructured": {
            // REQUIRED: Deployment name for structured output models.
            // Used for extracting entity lists with guaranteed JSON structure.
            "DeploymentName": "<deployment-name>"
        },
        "Embedding": {
            // REQUIRED: Deployment name for embedding models.
            // Used for vector embeddings generation.
            "DeploymentName": "<deployment-name>"
        }
    }
}
```

## Migration Contract: Old → New Mapping

Users migrating from the old `OpenAIService` section should apply these transformations:

| Old Key Path | New Key Path | Notes |
|---|---|---|
| `OpenAIService` | `FoundryModels` | Section rename |
| `OpenAIService.Default.ServiceType` | *(removed)* | No longer needed |
| `OpenAIService.Default.AzureAuthenticationType` | `FoundryModels.Default.AuthenticationType` | Same values |
| `OpenAIService.Default.AzureOpenAIEndpoint` | `FoundryModels.Default.Endpoint` | Same URL |
| `OpenAIService.Default.AzureOpenAIKey` | `FoundryModels.Default.ApiKey` | Same value |
| `OpenAIService.Default.OpenAIKey` | *(removed)* | Direct OpenAI support removed |
| `OpenAIService.Default.TenantId` | `FoundryModels.Default.TenantId` | Unchanged |
| `OpenAIService.Default.AzureOpenAIAppId` | *(removed)* | Not used with Foundry |
| `OpenAIService.ChatCompletion.AzureOpenAIDeploymentId` | `FoundryModels.ChatCompletion.DeploymentName` | Same value |
| `OpenAIService.ChatCompletion.ModelId` | *(removed)* | OpenAI-only, not needed |
| `OpenAIService.ChatCompletionStructured.AzureOpenAIDeploymentId` | `FoundryModels.ChatCompletionStructured.DeploymentName` | Same value |
| `OpenAIService.ChatCompletionStructured.ModelId` | *(removed)* | OpenAI-only, not needed |
| `OpenAIService.Embedding.AzureOpenAIDeploymentId` | `FoundryModels.Embedding.DeploymentName` | Same value |
| `OpenAIService.Embedding.ModelId` | *(removed)* | OpenAI-only, not needed |

## C# Interface Contract: IChatClientFactory (unchanged)

```csharp
public interface IChatClientFactory
{
    IChatClient CreateChatClient();
    IChatClient CreateStructuredOutputChatClient();
    IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator();
}
```

This interface remains identical. Consumers of `IChatClientFactory` require **zero changes**.
