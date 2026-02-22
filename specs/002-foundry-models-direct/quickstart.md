# Quickstart: Migrate to Microsoft Foundry Models Direct

**Feature**: `002-foundry-models-direct` | **Date**: 2026-02-22

## For Existing Users (Migration)

### Step 1: Update your `settings.json`

Replace the `OpenAIService` section with the new `FoundryModels` section:

**Before:**

```jsonc
"OpenAIService": {
    "Default": {
        "ServiceType": "AzureOpenAI",
        "AzureAuthenticationType": "EntraIdAuthentication",
        "AzureOpenAIEndpoint": "https://myresource.openai.azure.com/"
    },
    "ChatCompletion": {
        "AzureOpenAIDeploymentId": "gpt-4-1"
    },
    "ChatCompletionStructured": {
        "AzureOpenAIDeploymentId": "gpt-4-1"
    },
    "Embedding": {
        "AzureOpenAIDeploymentId": "text-embedding-ada-002"
    }
}
```

**After:**

```jsonc
"FoundryModels": {
    "Default": {
        "AuthenticationType": "EntraIdAuthentication",
        "Endpoint": "https://myresource.openai.azure.com/"
    },
    "ChatCompletion": {
        "DeploymentName": "gpt-4-1"
    },
    "ChatCompletionStructured": {
        "DeploymentName": "gpt-4-1"
    },
    "Embedding": {
        "DeploymentName": "text-embedding-ada-002"
    }
}
```

### Step 2: Remove the old section

Delete the entire `OpenAIService` section from your `settings.json`. If you leave it in, the application will display an error message guiding you to complete the migration.

### Step 3: Verify

Run any command that uses AI (e.g., `enrich-model`) to verify your configuration works:

```bash
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/ -- enrich-model --project /path/to/your/project
```

## For New Users

Run `init-project` to generate a new settings file with the `FoundryModels` section already configured:

```bash
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/ -- init-project --project /path/to/new/project
```

Then edit the generated `settings.json` and fill in your Microsoft Foundry endpoint and deployment names.

## Endpoint URL Formats

Any of these three URL formats are accepted for the `Endpoint` field:

| Format | Example |
|--------|---------|
| Foundry AI Services | `https://myresource.services.ai.azure.com/` |
| Azure OpenAI | `https://myresource.openai.azure.com/` |
| Cognitive Services | `https://myresource.cognitiveservices.azure.com/` |

## Authentication Options

| Type | Config Value | What You Need |
|------|-------------|---------------|
| Entra ID (recommended) | `"EntraIdAuthentication"` | RBAC role on the Foundry resource (e.g., Cognitive Services OpenAI Contributor) |
| API Key | `"ApiKey"` | Set `ApiKey` field in Default section |

## Key Changes Summary

| What Changed | Old | New |
|---|---|---|
| Settings section name | `OpenAIService` | `FoundryModels` |
| Endpoint property | `AzureOpenAIEndpoint` | `Endpoint` |
| Deployment property | `AzureOpenAIDeploymentId` | `DeploymentName` |
| Auth type property | `AzureAuthenticationType` | `AuthenticationType` |
| API key property | `AzureOpenAIKey` | `ApiKey` |
| Service type toggle | `ServiceType` | *(removed — Foundry only)* |
| OpenAI key | `OpenAIKey` | *(removed — no direct OpenAI)* |
| Model ID | `ModelId` | *(removed — use deployment names)* |
