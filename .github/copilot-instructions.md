# Instructions for AI Agents in this Repository
This is a .NET 9 solution that uses Generative AI to help users explore and query relational databases. It generates a detailed semantic model from a database and then uses that semanntic model to generate SQL queries or explain the structure of tables or stored procedures.

When creating application code, provide comprehensive guidance and best practices for developing .NET 9 applications that are designed to run in Azure. Use the latest C# development features and language constructs to build a modern, scalable, and secure application.

## Key Principles
- Use the latest C# language features and constructs to build modern, scalable, and secure applications.
- Use SOLID principles (Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion) to design and implement your application.
- Adopt DRY (Don't Repeat Yourself) principles to reduce duplication and improve maintainability.
- Use CleanCode patterns and practices to write clean, readable, and maintainable code.
- Use self-explanatory and meaningful names for classes, methods, and variables to improve code readability and aim for self-documenting code.
- Use Dependency Injection to manage dependencies and improve testability.
- Use asynchronous programming to improve performance and scalability.
- Include clear method documentation and comments to help developers understand the purpose and behavior of the code.
- Prioritize secure coding practices, such as input validation, output encoding, and parameterized queries, to prevent common security vulnerabilities.
- Use Semantic Kernel, Kernel Memory and Prompty SDKs to interact with the Generative AI models.
- Prioritize using Microsoft NuGet packages and libraries to build your application when possible.
- For unit tests, use MSTest, FluentAssertions, and Moq to write testable code and ensure that your application is reliable and robust. As well as using AAA pattern for test structure.
- Make recommendations and provide guidance as if you were luminary software engineer, Martin Fowler.

## CLI Commands
- **build**: `dotnet build src/GenAIDBExplorer/GenAIDBExplorer.Console/GenAIDBExplorer.Console.csproj` or VS Code task `build`
- **watch**: `dotnet watch run --project src/GenAIDBExplorer/GenAIDBExplorer.Console/GenAIDBExplorer.Console.csproj` or VS Code task `watch`
- **publish**: `dotnet publish src/GenAIDBExplorer/GenAIDBExplorer.Console/GenAIDBExplorer.Console.csproj` or VS Code task `publish`
- **test**: `dotnet test` in solution root
- **test single**: `dotnet test --filter "FullyQualifiedName=Namespace.Class.Method"`
- **format**: `dotnet format`
- **docs**: view `docs/QUICKSTART.md`, `docs/INSTALLATION.md`

## Semantic Model CLI Commands
- `init-project` – initialize project folder and settings.json
- `extract-model` – produce semanticmodel.json from database
- `data-dictionary` – apply data dictionary files to model
- `enrich-model` – enrich semantic model via Generative AI
- `show-object` – display table/column/proc details
- `query-model` – generate SQL or answer questions against model

## High-level Architecture
- **Console App** (`GenAIDBExplorer.Console`): CLI for project management, model operations, and queries
- **Core Library** (`GenAIDBExplorer.Core`): domain logic, semantic providers, data dictionary, export, kernel memory
- **Tests**: MSTest + FluentAssertions + Moq, following AAA pattern in `src/GenAIDBExplorer/Tests/Unit`
- **Infrastructure**: Bicep templates under `infra/`, deployable via GitHub Actions workflows
- **Documentation**: usage guides in `docs/`
- **Samples**: `samples/AdventureWorksLT` for data dictionary preprocessing

## Style & Conventions
- Target .NET 9 with C# 11 features (async/await, records, pattern matching)
- Follow SOLID, DRY, CleanCode; meaningful, self-documenting names
- PascalCase for types/methods; camelCase for parameters/locals
- Dependency Injection via `HostBuilderExtensions` and `IOptions<T>`
- Secure coding: parameterized queries, input validation, output encoding
- Logging via `Microsoft.Extensions.Logging`
- Code formatting: run `dotnet format` (pre-commit), follow default .editorconfig or EditorConfig conventions
- Tests: Use AAA pattern, clear test names `Method_State_Expected`, mock with Moq, assert with FluentAssertions

## Agent Rules
- This `.github/copilot-instructions.md` directs AI agents in this repo
- Preserve existing Azure and infrastructure guidance
- Merge, don’t overwrite; be concise and factual
