<!--
=============================================================================
SYNC IMPACT REPORT
=============================================================================
Version Change: 1.2.0 → 1.3.0
Rationale: Replaced Semantic Kernel with Microsoft.Extensions.AI and Microsoft Agent Framework to reflect actual project state after migration

Modified Principles:
- II. AI Integration via Semantic Kernel → II. AI Integration via Microsoft.Extensions.AI

Modified Sections:
- Principle II: Replaced ISemanticKernelFactory with IChatClientFactory/AIAgent, .prompty with .prompt, Core/Prompty with Core/PromptTemplates
- Principle VII: Replaced ISemanticKernelFactory reference with IChatClientFactory
- Technology Stack & Standards → Required Technologies: Replaced Semantic Kernel with Microsoft.Extensions.AI + Microsoft Agent Framework

Added Sections: None

Removed Sections: None

Templates Status:
⚠️ .github/copilot-instructions.md - Should be verified for SK references
⚠️ AGENTS.md - Should be verified for SK references

Code Impact: None (project already migrated from SK to M.E.AI)

Follow-up TODOs:
- Verify .github/copilot-instructions.md and AGENTS.md have no stale Semantic Kernel references

Generated: 2026-02-28
Previous Version Generated: 2026-02-23
=============================================================================
-->

# GenAI Database Explorer Constitution

## Core Principles

### I. Semantic Model Integrity

The semantic model is the central artifact of this project and MUST maintain consistency, accuracy, and traceability.

- **Semantic models MUST be version controlled** and treated as deployable assets
- **Schema extraction MUST be idempotent** - repeated extractions produce identical results for unchanged databases
- **Enrichment operations MUST preserve provenance** - track which descriptions are AI-generated vs. user-provided vs. data dictionary sourced
- **All semantic model changes MUST be auditable** through change tracking mechanisms
- **Semantic models MUST be independently validatable** without requiring database access

**Rationale**: The semantic model is the core value proposition - it enables database understanding without direct access, supports versioning of database documentation, and provides a foundation for natural language queries. Integrity violations would undermine the entire tool's purpose.

### II. AI Integration via Microsoft.Extensions.AI

ALL AI operations MUST use `IChatClientFactory` for direct AI operations (chat completion, embeddings) and `AIAgent` from the Microsoft Agent Framework for agent orchestration.

- **AI prompts MUST be stored in `.prompt` files** under `Core/PromptTemplates/` directory
- **Token usage MUST be tracked** via `response.Usage` (`UsageDetails` with `long?` properties) for cost management
- **AI operations MUST use structured logging** with appropriate scopes for debugging
- **Prompt template pattern MUST follow existing provider implementations** using `IPromptTemplateParser` and `ILiquidTemplateRenderer` for consistency
- **AI service configuration MUST support Azure AI Foundry** via `FoundryModels` settings
- **Agent orchestration MUST use the Microsoft Agent Framework** (`AIAgent`, `AIFunctionFactory`, `RunStreamingAsync`) for multi-step reasoning tasks

**Rationale**: Centralized AI integration via `IChatClientFactory` ensures consistent error handling, token tracking, cost management, and testability. The Agent Framework provides managed ReAct loops for agent scenarios, abstracting the tool-call orchestration cycle. Both patterns coexist: `IChatClientFactory` for direct AI calls (enrichment, embeddings), `AIAgent` for autonomous agent workflows (query-model).

### III. Repository Pattern for Persistence

Semantic models MUST be persistable to multiple backends without coupling domain logic to storage.

- **All persistence MUST use `ISemanticModelRepository` abstraction**
- **Three strategies MUST be supported**: LocalDisk, AzureBlob, CosmosDB
- **Repository implementations MUST be independently testable** via contract tests
- **Storage format MUST be JSON** with clear schema versioning
- **Repository selection MUST be configuration-driven** via `settings.json`

**Rationale**: Different deployment scenarios require different storage strategies. LocalDisk for development/samples, AzureBlob for simple cloud deployment, CosmosDB for advanced scenarios with vector search. The abstraction enables testing and future storage backend additions.

### IV. Project-Based Workflow

All operations are scoped to a project folder containing `settings.json` that drives behavior.

- **Every command MUST accept `-p/--project` parameter** for project folder path
- **Project settings MUST be strongly-typed** via `IProject.Settings` interface
- **Settings MUST validate on load** with clear error messages for invalid configuration
- **Project folders MUST be self-contained** - all artifacts relative to project root
- **Sample projects MUST be provided** demonstrating valid configurations

**Rationale**: Project-based workflow enables multi-database scenarios, reproducible operations, and clear artifact organization. Settings-driven behavior makes the tool configurable without code changes.

### V. Test-First Development (NON-NEGOTIABLE)

Tests MUST be written before implementation and MUST fail before code is written.

- **Unit tests MUST use MSTest + FluentAssertions + Moq** for consistency
- **Test naming MUST follow `Method_State_Expected` pattern** for clarity
- **AAA pattern MUST be used**: Arrange, Act, Assert
- **Integration tests MUST be PowerShell-based** and runnable via `.github/scripts/Invoke-IntegrationTests.ps1`
- **Code coverage MUST be measured** but not enforced with arbitrary thresholds

**Rationale**: The project involves complex interactions between database schema extraction, AI enrichment, and persistence. Test-first development ensures these integrations work correctly and prevents regressions.

### VI. CLI-First Interface

Functionality MUST be exposed via System.CommandLine with clear, composable commands.

- **Command handlers MUST follow the static factory pattern**: `SetupCommand(IHost host)`
- **CLI parameters MUST use kebab-case** for multi-word options (e.g., `--schema-name`, `--skip-tables`, `--output-file-name`) per Microsoft's System.CommandLine design guidance
- **Commands MUST support `--help`** with comprehensive usage information
- **Error messages MUST be actionable** - tell users how to fix the problem
- **Commands MUST write results to stdout** and errors to stderr
- **Long-running operations MUST show progress** to prevent user confusion

**Rationale**: CLI-first design enables scripting, CI/CD integration, and automation. System.CommandLine provides consistent help, parsing, and validation. Kebab-case naming follows Microsoft's official guidance for .NET CLI tools and improves consistency across the .NET ecosystem.

### VII. Dependency Injection & Configuration

All services MUST be registered via `HostBuilderExtensions.ConfigureHost()` for consistency.

- **Singletons MUST be used** for core services (`IChatClientFactory`, `IProject`)
- **Decorators MUST be used** for cross-cutting concerns (caching, performance monitoring)
- **Configuration MUST load from** `appsettings.json` in console project
- **Options pattern MUST be used** via `IOptions<T>` for configuration sections
- **Service lifetimes MUST be explicit** and documented in registration

**Rationale**: Centralized DI configuration ensures consistent service lifetimes, enables testing via mock injection, and makes cross-cutting concerns (logging, caching) composable via decorators.

## Technology Stack & Standards

### Required Technologies

- **.NET 10** with C# 14 features (async/await, records, pattern matching, primary constructors, collection expressions)
- **Microsoft.Extensions.AI** (`IChatClient`, `IEmbeddingGenerator`) for AI integration
- **Microsoft Agent Framework** (`Microsoft.Agents.AI`) for agent orchestration
- **System.CommandLine** (migrating to Beta5) for CLI
- **Microsoft.Extensions.** libraries for DI, Configuration, Logging
- **MSTest + FluentAssertions + Moq** for testing
- **Bicep** for Azure infrastructure as code

### Code Style (NON-NEGOTIABLE)

- **PascalCase** for types, methods, properties
- **camelCase** for parameters, local variables, private fields
- **SOLID principles** MUST be followed - especially Single Responsibility and Dependency Inversion
- **DRY principle** MUST be applied - extract common logic to shared utilities
- **Meaningful names** MUST be used - no abbreviations unless industry-standard
- **`dotnet format` MUST pass** before commits - use `format-fix-whitespace-only` task after C# file changes

### Security Requirements

- **Parameterized queries MUST be used** for all database operations - no string concatenation
- **Input validation MUST occur** at command handler boundaries
- **Output encoding MUST be applied** appropriately for context (JSON, Markdown, SQL)
- **Managed identity MUST be preferred** over API keys for Azure resources
- **Secrets MUST NOT be committed** - use user secrets or Azure Key Vault

### Naming Conventions

- **Never abbreviate "Cosmos DB"** to just "Cosmos" - always use `CosmosDb`, `CosmosDB`, or `COSMOS_DB` depending on context (C# class name, variable, constant)
- **Repository implementations MUST be suffixed** with `Repository` (e.g., `LocalDiskSemanticModelRepository`)
- **Command handlers MUST be suffixed** with `CommandHandler` (e.g., `ExtractModelCommandHandler`)
- **Test files MUST be suffixed** with `Tests.cs` (e.g., `SemanticModelTests.cs`)

## Development Workflow

### Pre-Implementation

1. **Read existing specifications** in `spec/` directory for relevant features
1. **Check existing plans** in `plan/` directory for architectural decisions
1. **Review project documentation** in `docs/` for component behavior
1. **Validate against constitution** - ensure proposed changes align with principles

### Implementation

1. **Create feature branch** following naming convention `###-feature-name`
1. **Write tests first** - ensure they fail before implementation
1. **Implement minimum code** to make tests pass
1. **Run `format-fix-whitespace-only`** task after any C# file changes
1. **Verify all tests pass** via `dotnet test`
1. **Run integration tests** if touching CLI commands or database operations
1. **Update documentation** if public APIs or CLI commands change

### Quality Gates

- **All unit tests MUST pass** before commit
- **Code formatting MUST be clean** via `dotnet format --verify-no-changes`
- **Integration tests MUST pass** for CLI command changes
- **No new compiler warnings** introduced
- **Semantic model backwards compatibility** MUST be maintained or migration path provided

### Documentation Requirements

- **Public APIs MUST have XML comments** describing purpose, parameters, returns, exceptions
- **CLI commands MUST update** relevant documentation in `docs/cli/`
- **Component changes MUST update** relevant documentation in `docs/components/`
- **Architectural decisions MUST be documented** in `plan/` directory
- **README.md and AGENTS.md MUST stay synchronized** with major changes

## Governance

### Constitution Authority

- This constitution supersedes all other guidance when conflicts arise
- AGENTS.md and .github/copilot-instructions.md MUST align with constitution principles
- Violations of NON-NEGOTIABLE principles MUST be rejected in code review
- Complexity additions MUST be justified with clear reasoning in plan documents

### Amendment Process

1. **Propose amendment** via issue or pull request with rationale
1. **Version bump follows semantic versioning**:
   - **MAJOR**: Backward incompatible principle changes or removals
   - **MINOR**: New principles added or substantial expansions
   - **PATCH**: Clarifications, wording improvements, non-semantic fixes
1. **Update sync impact report** at top of constitution file
1. **Propagate changes** to affected templates, commands, and documentation
1. **Ratification date remains** original adoption date
1. **Last amended date becomes** date of amendment approval

### Compliance Reviews

- **Pull request reviews MUST verify** alignment with constitution principles
- **Quarterly reviews SHOULD occur** to assess if constitution needs updates
- **Template consistency MUST be checked** when constitution changes
- **Agent guidance files MUST be updated** within same PR as constitution amendments

### Runtime Guidance

For day-to-day development guidance beyond constitutional rules, refer to:

- **AGENTS.md** for agent-specific development patterns and commands
- **.github/copilot-instructions.md** for Copilot-specific integration patterns
- **docs/** directory for component-specific technical guidance

**Version**: 1.2.0 | **Ratified**: 2025-11-25 | **Last Amended**: 2026-02-23
