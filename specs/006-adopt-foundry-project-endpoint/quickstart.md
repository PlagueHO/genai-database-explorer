# Quickstart: Adopt Microsoft Foundry Project Endpoint

**Feature Branch**: `006-adopt-foundry-project-endpoint`
**Generated**: 2026-03-01

## Prerequisites

- .NET 10 SDK installed
- Azure subscription with a Microsoft Foundry (new) resource
- A Foundry project created (manually or via `azd up` after Bicep updates)
- Chat completion model deployed (e.g., `gpt-4o`, `gpt-5.2-chat`)
- Embedding model deployed (e.g., `text-embedding-3-large`)
- Appropriate role assignment on the Foundry resource (e.g., `Cognitive Services OpenAI User`)

## 1. Get Your Project Endpoint

From the Azure portal → Microsoft Foundry resource → Projects → select your project → Overview → copy the **Project endpoint**.

The endpoint format is:

```
https://<resource-name>.services.ai.azure.com/api/projects/<project-name>
```

> **Important**: The endpoint must include the `/api/projects/<project-name>` path. A resource-level endpoint (`https://<resource>.services.ai.azure.com/`) will NOT work.

## 2. Initialize a New Project

```bash
cd genai-database-explorer-service

dotnet run --project src/GenAIDBExplorer.Console/ -- init-project \
  -p /path/to/myproject \
  --foundry-endpoint "https://myresource.services.ai.azure.com/api/projects/myproject" \
  --foundry-auth-type EntraIdAuthentication \
  --foundry-chat-deployment gpt-4o \
  --foundry-embedding-deployment text-embedding-3-large
```

This generates a `settings.json` with the `MicrosoftFoundry` section:

```json
{
    "SettingsVersion": "2.0.0",
    "MicrosoftFoundry": {
        "Default": {
            "AuthenticationType": "EntraIdAuthentication",
            "Endpoint": "https://myresource.services.ai.azure.com/api/projects/myproject"
        },
        "ChatCompletion": {
            "DeploymentName": "gpt-4o"
        },
        "Embedding": {
            "DeploymentName": "text-embedding-3-large"
        }
    }
}
```

## 3. Migrating from Existing Projects

If you have a project with the old `FoundryModels` section, you must manually update your `settings.json`:

1. Rename the `"FoundryModels"` section to `"MicrosoftFoundry"`
2. Update the `"Endpoint"` value to a Foundry project endpoint (format: `https://<resource>.services.ai.azure.com/api/projects/<project-name>`)
3. Update `"SettingsVersion"` from `"1.0.0"` to `"2.0.0"`
4. Remove any `"ChatCompletionStructured"` sub-section (no longer needed)

**Before (v1.0.0)**:

```json
{
    "SettingsVersion": "1.0.0",
    "FoundryModels": {
        "Default": {
            "Endpoint": "https://myresource.openai.azure.com/"
        }
    }
}
```

**After (v2.0.0)**:

```json
{
    "SettingsVersion": "2.0.0",
    "MicrosoftFoundry": {
        "Default": {
            "Endpoint": "https://myresource.services.ai.azure.com/api/projects/myproject"
        }
    }
}
```

## 4. Verify the Configuration

Run any AI-powered command to verify the connection:

```bash
dotnet run --project src/GenAIDBExplorer.Console/ -- extract-model -p /path/to/myproject
dotnet run --project src/GenAIDBExplorer.Console/ -- enrich-model -p /path/to/myproject
```

If the project endpoint is invalid, you will see a clear error:

```
Error: The endpoint must be a Microsoft Foundry project endpoint in the format:
  https://<resource>.services.ai.azure.com/api/projects/<project-name>
Current endpoint: https://myresource.services.ai.azure.com/
```

## 5. Deploy Infrastructure (Optional)

If using the provided Bicep templates:

```bash
azd up
```

After deployment, the output will include:

```
AZURE_AI_FOUNDRY_PROJECT_ENDPOINT = https://myresource.services.ai.azure.com/api/projects/genaidbexplorer
```

Copy this value into your project's `settings.json` `MicrosoftFoundry.Default.Endpoint`.

## 6. Query with Foundry-Hosted Agent

After migration, the `query-model` command uses a Foundry-hosted agent:

```bash
dotnet run --project src/GenAIDBExplorer.Console/ -- query-model -p /path/to/myproject
```

The agent is created and managed through the Foundry Agent Service, providing managed infrastructure, tracing, and multi-round tool calling through the project endpoint.

## Development Notes

### Building and Testing

```bash
# Build
dotnet build GenAIDBExplorer.slnx

# Run unit tests
dotnet exec tests/unit/GenAIDBExplorer.Core.Test/bin/Debug/net10.0/GenAIDBExplorer.Core.Test.dll
dotnet exec tests/unit/GenAIDBExplorer.Console.Test/bin/Debug/net10.0/GenAIDBExplorer.Console.Test.dll

# Format
dotnet format GenAIDBExplorer.slnx
```

### Key Files to Understand

| File | Purpose |
|------|---------|
| `Core/ChatClients/ChatClientFactory.cs` | Creates `AIProjectClient`, `IChatClient`, `IEmbeddingGenerator` |
| `Core/Models/Project/MicrosoftFoundrySettings.cs` | Configuration model for `MicrosoftFoundry` section |
| `Core/Models/Project/Project.cs` | Settings loading, validation, legacy detection |
| `Core/SemanticModelQuery/SemanticModelQueryService.cs` | Foundry-hosted agent for natural language queries |
| `Console/CommandHandlers/InitProjectCommandHandler.cs` | CLI `init-project` with settings generation |
| `Core/DefaultProject/settings.json` | Template for new projects (v2.0.0) |
