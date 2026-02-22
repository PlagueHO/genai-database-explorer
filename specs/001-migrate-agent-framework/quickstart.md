# Quickstart: Migrate from Semantic Kernel to Microsoft Agent Framework

**Branch**: `001-migrate-agent-framework`

## Prerequisites

- .NET 10 SDK
- Azure AI services deployment (Azure OpenAI with chat completion + embedding models) OR OpenAI API key
- Existing GenAI Database Explorer project folder with `settings.json`

## New NuGet Packages

```xml
<!-- Add these to GenAIDBExplorer.Core.csproj -->
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="*" />
<PackageReference Include="Azure.AI.OpenAI" Version="*" />
<PackageReference Include="Scriban" Version="*" />

<!-- Remove ALL of these from GenAIDBExplorer.Core.csproj -->
<PackageReference Include="Microsoft.SemanticKernel" />
<PackageReference Include="Microsoft.SemanticKernel.Abstractions" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.AzureOpenAI" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" />
<PackageReference Include="Microsoft.SemanticKernel.Core" />
<PackageReference Include="Microsoft.SemanticKernel.PromptTemplates.Handlebars" />
<PackageReference Include="Microsoft.SemanticKernel.PromptTemplates.Liquid" />
<PackageReference Include="Microsoft.SemanticKernel.Prompty" />
<PackageReference Include="Microsoft.SemanticKernel.Yaml" />

<!-- RETAIN this package — it has NO dependency on SK core -->
<!-- Only depends on Microsoft.Extensions.AI.Abstractions, M.E.DI.Abstractions, M.E.VectorData.Abstractions, System.Numerics.Tensors -->
<PackageReference Include="Microsoft.SemanticKernel.Connectors.InMemory" />
```

## Configuration Changes (settings.json)

No breaking changes to `settings.json`. The existing `OpenAIService` configuration block continues to work:

```json
{
  "OpenAIService": {
    "Default": {
      "ServiceType": "AzureOpenAI",
      "AzureAuthenticationType": "EntraIdAuthentication",
      "AzureOpenAIEndpoint": "https://your-resource.openai.azure.com/",
      "TenantId": "optional-tenant-id"
    },
    "ChatCompletion": {
      "AzureOpenAIDeploymentId": "gpt-4o"
    },
    "ChatCompletionStructured": {
      "AzureOpenAIDeploymentId": "gpt-4o"
    },
    "Embedding": {
      "AzureOpenAIDeploymentId": "text-embedding-3-large"
    }
  }
}
```

## Key Migration Patterns

### Before (Semantic Kernel)

```csharp
// Creating AI service
var kernel = _semanticKernelFactory.CreateSemanticKernel();
var function = kernel.CreateFunctionFromPromptyFile(promptyFilename);
var result = await kernel.InvokeAsync(function, arguments);
var usage = result.Metadata?["Usage"] as ChatTokenUsage;
```

### After (Microsoft.Extensions.AI)

```csharp
// Creating AI service
var chatClient = _chatClientFactory.CreateChatClient();
var template = _templateParser.ParseFromFile(promptFilename);
var messages = _templateRenderer.RenderMessages(template, variables);
var response = await chatClient.GetResponseAsync(messages, chatOptions);
var usage = response.Usage; // InputTokenCount, OutputTokenCount
```

### Structured Output Pattern

```csharp
// Before (SK)
var settings = new OpenAIPromptExecutionSettings { ResponseFormat = typeof(TableList) };

// After (Microsoft.Extensions.AI)
var options = new ChatOptions
{
    ResponseFormat = ChatResponseFormat.ForJsonSchema<TableList>()
};
var response = await chatClient.GetResponseAsync(messages, options);
var tableList = JsonSerializer.Deserialize<TableList>(response.Text);
```

## Running Tests

```bash
# Build first to verify no SK references remain
dotnet build src/GenAIDBExplorer/GenAIDBExplorer.slnx

# Run all unit tests
dotnet test --solution src/GenAIDBExplorer/GenAIDBExplorer.slnx

# Verify no SK references
grep -r "Microsoft.SemanticKernel" src/GenAIDBExplorer/ --include="*.cs" --include="*.csproj"
# Should return ONLY references to Microsoft.SemanticKernel.Connectors.InMemory (retained)

# Verify no SKEXP pragmas
grep -r "SKEXP" src/GenAIDBExplorer/ --include="*.cs"
# Should return zero results
```

## Implementation Order

1. **New infrastructure** (no breaking changes): ChatClientFactory, PromptTemplateParser, LiquidTemplateRenderer — with tests
1. **Migrate providers**: Update SemanticDescriptionProvider + DataDictionaryProvider to use new infrastructure — update tests
1. **Swap vector store**: ~~Change DI from Sk* to non-SK InMemory implementations~~ NOT NEEDED — `Microsoft.SemanticKernel.Connectors.InMemory` retained (no SK core dep)
1. **Migrate embedding**: Replace SemanticKernelEmbeddingGenerator with ChatClientEmbeddingGenerator
1. **Update DI**: Replace all SK registrations in HostBuilderExtensions
1. **Delete SK code**: Remove SemanticKernel/ directory, SemanticKernelEmbeddingGenerator (InMemory vector store files retained)
1. **Remove SK packages**: Clean csproj files, verify zero SK core references (`Microsoft.SemanticKernel.Connectors.InMemory` is retained)
1. **Rename prompt files**: `.prompty` → `.prompt`, move `Prompty/` → `PromptTemplates/`
1. **Amend constitution**: Update Principle II references
