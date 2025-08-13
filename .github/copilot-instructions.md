# Instructions for AI Agents in this Repository

This is a .NET 9 solution that uses Generative AI to help users explore and query relational databases. It creates a **semantic model** from database schemas, enriches it with AI-generated descriptions, and enables natural language querying.

## Core Architecture & Data Flow

The application follows a **project-based workflow** where each database analysis is contained in a project folder with `settings.json`:

1. **Extract Phase**: `ISemanticModelProvider` + `SchemaRepository` extract raw schema → `semanticmodel.json`
2. **Enrich Phase**: `SemanticDescriptionProvider` uses Prompty files + `SemanticKernelFactory` to generate AI descriptions
3. **Query Phase**: Natural language questions → SQL generation via Semantic Kernel
4. **Persistence**: Multiple strategies (LocalDisk/AzureBlob/CosmosDB) via `ISemanticModelRepository`

### Key Components

- **Semantic Model**: Core domain object (`SemanticModel.cs`) with lazy loading, change tracking, and caching
- **Command Handlers**: System.CommandLine-based CLI in `GenAIDBExplorer.Console/CommandHandlers/`
- **Semantic Providers**: AI-powered enrichment services using Prompty templates in `Core/Prompty/`
- **Repository Pattern**: Abstract persistence with multiple backends (LocalDisk/AzureBlob/CosmosDB)
- **Project Settings**: JSON-based configuration driving all operations (`samples/AdventureWorksLT/settings.json`)

## Critical Patterns

### AI Integration (Semantic Kernel + Prompty)
```csharp
// ALL AI operations use SemanticKernelFactory.CreateSemanticKernel()
public class SemanticDescriptionProvider(
    ISemanticKernelFactory semanticKernelFactory, // <- Always inject this
    ILogger<SemanticDescriptionProvider> logger)
{
    private async Task<string> ProcessWithPromptyAsync(string promptyFile)
    {
        var kernel = _semanticKernelFactory.CreateSemanticKernel(); // <- Standard pattern
        var function = kernel.CreateFunctionFromPromptyFile(promptyFilename);
        var result = await kernel.InvokeAsync(function, arguments);
        // Track tokens: result.Metadata?["Usage"] as ChatTokenUsage
    }
}
```

### Dependency Injection Setup
All services registered in `HostBuilderExtensions.ConfigureHost()`:
- Singletons for core services (`ISemanticKernelFactory`, `IProject`)
- Decorated providers with caching/performance monitoring
- Configuration loaded from console project's `appsettings.json`

### Command Handler Pattern
```csharp
public class ExtractModelCommandHandler : CommandHandler<ExtractModelCommandHandlerOptions>
{
    public static Command SetupCommand(IHost host) // <- Static factory pattern
    {
        var command = new Command("extract-model");
        command.SetHandler(async (options) => {
            var handler = host.Services.GetRequiredService<ExtractModelCommandHandler>();
            await handler.HandleAsync(options);
        });
    }
}
```

## Essential Development Commands

```bash
# VS Code tasks (preferred)
Ctrl+Shift+P → "Tasks: Run Task" → build/watch/test/publish

# Direct commands
dotnet build src/GenAIDBExplorer/GenAIDBExplorer.Console/
dotnet watch run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/
dotnet test  # From solution root

# CLI operations (require project folder)
dotnet run --project GenAIDBExplorer.Console/ -- init-project -p d:/temp
dotnet run --project GenAIDBExplorer.Console/ -- extract-model -p d:/temp
dotnet run --project GenAIDBExplorer.Console/ -- enrich-model -p d:/temp
```

## Project Structure Conventions

```
src/GenAIDBExplorer/
├── GenAIDBExplorer.Console/        # CLI app, command handlers, DI setup
├── GenAIDBExplorer.Core/           # Domain logic, providers, models
│   ├── Models/SemanticModel/       # Core domain objects  
│   ├── Prompty/                    # AI prompt templates (.prompty files)
│   ├── SemanticProviders/          # AI enrichment services
│   └── Repository/                 # Persistence abstractions
└── Tests/Unit/                     # MSTest + FluentAssertions + Moq

# Working directories (project folders)
samples/AdventureWorksLT/
├── settings.json                   # Project configuration
├── SemanticModel/                  # Generated semantic models
└── DataDictionary/                 # Optional enrichment data
```

## Style & Conventions

- Ensure new/changed code is indented correctly
- Target .NET 9 with C# 11 features (async/await, records, pattern matching)
- Follow SOLID, DRY, CleanCode; meaningful, self-documenting names
- PascalCase for types/methods; camelCase for parameters/locals
- Dependency Injection via `HostBuilderExtensions` and `IOptions<T>`
- Secure coding: parameterized queries, input validation, output encoding
- Logging via `Microsoft.Extensions.Logging`
- Code formatting: run `dotnet format` (pre-commit), follow default .editorconfig or EditorConfig conventions
- Tests: Use AAA pattern, clear test names `Method_State_Expected`, mock with Moq, assert with FluentAssertions
- Never refer to Cosmos DB as just `Cosmos`. It should always be `CosmosDb` or `CosmosDB` or `COSMOS_DB` depending on usecase.

## Agent Rules

- This `.github/copilot-instructions.md` directs AI agents in this repo
- Preserve existing Azure and infrastructure guidance
- Merge, don’t overwrite; be concise and factual
- After making changes to any `*.cs` file, always run VS Code task `format-fix-whitespace-only` task

## Test

- Use `dotnet test` to run all tests
- Test files should be named `*Tests.cs` and located in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.*.Test/`
- Use MSTest, FluentAssertions, and Moq for unit tests
- Use AAA pattern for test structure: Arrange, Act, Assert
- Use `Should().BeTrue()` for boolean assertions
- Use `Should().BeEquivalentTo()` for object comparisons

## Configuration & Settings Management

The `settings.json` file drives all operations:
- **Database**: Connection string, schema, parallelism settings
- **OpenAIService**: Azure OpenAI endpoints, model deployments, API keys
- **SemanticModelRepository**: Persistence strategy (LocalDisk/AzureBlob/CosmosDB)
- **DataDictionary**: Column type mappings, enrichment rules

Key pattern: `IProject.Settings` provides strongly-typed access to all configuration.

## AI/LLM Integration Requirements

- **ALWAYS** use `ISemanticKernelFactory.CreateSemanticKernel()` for AI operations
- Store AI prompts in `.prompty` files under `Core/Prompty/`
- Track token usage: `result.Metadata?["Usage"] as ChatTokenUsage`
- Use structured logging with scopes for AI operations
- Follow SemanticDescriptionProvider pattern for prompt execution

## Infrastructure & Deployment

- **Bicep templates**: `infra/main.bicep` deploys Azure OpenAI, optional AI Search, CosmosDB, Storage
- **GitHub Actions**: CI/CD in `.github/workflows/`
- **Azure resources**: Managed identity authentication preferred over API keys