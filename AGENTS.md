# AGENTS.md

This is a .NET 9 solution that uses Generative AI to help users explore and query relational databases. It creates a **semantic model** from database schemas, enriches it with AI-generated descriptions, and enables natural language querying.

## Setup commands

```bash
# Build the solution
dotnet build src/GenAIDBExplorer/GenAIDBExplorer.sln

# Watch mode for development
dotnet watch run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/

# Run unit tests
dotnet test

# Run integration tests  
pwsh -Command "New-Item -ItemType Directory -Path './test-results' -Force | Out-Null; & ./.github/scripts/Invoke-IntegrationTests.ps1"

# Format code
dotnet format src/GenAIDBExplorer/GenAIDBExplorer.sln
```

## Project structure

```text
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

## Core architecture

The application follows a **project-based workflow** where each database analysis is contained in a project folder with `settings.json`:

1. **Extract Phase**: `ISemanticModelProvider` + `SchemaRepository` extract raw schema → `semanticmodel.json`
2. **Enrich Phase**: `SemanticDescriptionProvider` uses Prompty files + `SemanticKernelFactory` to generate AI descriptions
3. **Query Phase**: Natural language questions → SQL generation via Semantic Kernel
4. **Persistence**: Multiple strategies (LocalDisk/AzureBlob/CosmosDB) via `ISemanticModelRepository`

### Key components

- **Semantic Model**: Core domain object (`SemanticModel.cs`) with lazy loading, change tracking, and caching
- **Command Handlers**: System.CommandLine-based CLI in `GenAIDBExplorer.Console/CommandHandlers/`
- **Semantic Providers**: AI-powered enrichment services using Prompty templates in `Core/Prompty/`
- **Repository Pattern**: Abstract persistence with multiple backends (LocalDisk/AzureBlob/CosmosDB)
- **Project Settings**: JSON-based configuration driving all operations (`samples/AdventureWorksLT/settings.json`)

## CLI operations

All CLI operations require a project folder with `settings.json`:

```bash
# Initialize a new project
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/ -- init-project -p d:/temp/myproject

# Extract database schema
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/ -- extract-model -p d:/temp/myproject

# Enrich with AI descriptions
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/ -- enrich-model -p d:/temp/myproject

# Query with natural language
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/ -- query-model -p d:/temp/myproject

# Export model to markdown
dotnet run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/ -- export-model -p d:/temp/myproject --outputPath output.md
```

## Code style

- Target .NET 9 with C# 11 features (async/await, records, pattern matching)
- Follow SOLID, DRY, CleanCode principles; meaningful, self-documenting names
- PascalCase for types/methods; camelCase for parameters/locals
- Dependency Injection via `HostBuilderExtensions` and `IOptions<T>`
- Secure coding: parameterized queries, input validation, output encoding
- Logging via `Microsoft.Extensions.Logging`
- Never refer to Cosmos DB as just `Cosmos`. Always use `CosmosDb`, `CosmosDB`, or `COSMOS_DB`

## AI/LLM integration patterns

### Critical: Always use SemanticKernelFactory

```csharp
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

### AI prompt storage

- Store AI prompts in `.prompty` files under `Core/Prompty/`
- Track token usage: `result.Metadata?["Usage"] as ChatTokenUsage`
- Use structured logging with scopes for AI operations
- Follow SemanticDescriptionProvider pattern for prompt execution

## Testing instructions

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.Core.Test/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Integration tests (requires PowerShell)
pwsh ./.github/scripts/Invoke-IntegrationTests.ps1
```

### Test conventions

- Use MSTest, FluentAssertions, and Moq for unit tests
- Test files named `*Tests.cs` in `src/GenAIDBExplorer/Tests/Unit/GenAIDBExplorer.*.Test/`
- Use AAA pattern: Arrange, Act, Assert
- Use `Should().BeTrue()` for boolean assertions
- Use `Should().BeEquivalentTo()` for object comparisons
- Clear test names: `Method_State_Expected`

## Dependency injection setup

All services registered in `HostBuilderExtensions.ConfigureHost()`:

- Singletons for core services (`ISemanticKernelFactory`, `IProject`)
- Decorated providers with caching/performance monitoring
- Configuration loaded from console project's `appsettings.json`

## Command handler pattern

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

## Configuration management

The `settings.json` file drives all operations:

- **Database**: Connection string, schema, parallelism settings
- **OpenAIService**: Azure OpenAI endpoints, model deployments, API keys
- **SemanticModelRepository**: Persistence strategy (LocalDisk/AzureBlob/CosmosDB)
- **DataDictionary**: Column type mappings, enrichment rules

Key pattern: `IProject.Settings` provides strongly-typed access to all configuration.

## Infrastructure & deployment

- **Bicep templates**: `infra/main.bicep` deploys Azure OpenAI, optional AI Search, CosmosDB, Storage
- **GitHub Actions**: CI/CD in `.github/workflows/`
- **Azure resources**: Managed identity authentication preferred over API keys

## Build commands

Use VS Code tasks (preferred) via `Ctrl+Shift+P` → "Tasks: Run Task":

- `dotnet-build-solution`: Build the entire solution
- `dotnet-watch-console`: Watch mode for console app
- `dotnet-test-unit`: Run unit tests
- `format-fix-whitespace-only`: Fix code formatting
- `test-console-integration`: Run integration tests

## Security considerations

- Use parameterized queries for all database operations
- Validate all user inputs before processing
- Encode outputs appropriately for their context
- Prefer managed identity over API keys for Azure resources
- Store sensitive configuration in Azure Key Vault or user secrets

## Performance guidelines

- Use lazy loading patterns for semantic models
- Implement caching for expensive operations (AI calls, database queries)
- Track and log token usage for AI operations
- Use structured logging with appropriate scopes
- Monitor performance with decorators on key services

## Post-edit requirements

After making changes to any `*.cs` file, always run the VS Code task `format-fix-whitespace-only` to ensure consistent formatting.
