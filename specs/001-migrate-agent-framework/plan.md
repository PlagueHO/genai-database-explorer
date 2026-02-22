# Implementation Plan: Migrate from Semantic Kernel to Microsoft Agent Framework

**Branch**: `001-migrate-agent-framework` | **Date**: 2026-02-20 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-migrate-agent-framework/spec.md`

## Summary

Migrate the GenAI Database Explorer from Semantic Kernel + Prompty to `Microsoft.Extensions.AI` abstractions with a custom prompt template pipeline. Replace `ISemanticKernelFactory` with `IChatClientFactory`, the Prompty file loader with a custom YAML+Liquid parser/renderer, and the SK embedding generator with a direct `IEmbeddingGenerator`. The `Microsoft.SemanticKernel.Connectors.InMemory` package is retained — research confirmed it has zero dependency on SK core libraries.

## Technical Context

**Language/Version**: .NET 10 / C# 14
**Primary Dependencies**: `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, `Azure.AI.Projects` (Microsoft Foundry SDK), `Azure.AI.Projects.OpenAI` (Foundry OpenAI integration), `Scriban`, `YamlDotNet` (existing), `Microsoft.SemanticKernel.Connectors.InMemory` (retained — no SK core dependency)
**Storage**: SQL Server (existing connection), LocalDisk/AzureBlob/CosmosDB repositories (unchanged)
**Testing**: MSTest + FluentAssertions 8.8.0 + Moq 4.20.72
**Target Platform**: Windows/Linux CLI application (.NET 10)
**Project Type**: Single .NET solution (Console + Core library + Tests)
**Performance Goals**: AI round-trip latency (1-30s) dominates; no regression from framework swap. Local operations (template parsing, rendering) must complete in <10ms.
**Constraints**: Zero CLI breaking changes. Backward-compatible model files. No re-enrichment or re-generation required.
**Scale/Scope**: ~15 source files directly affected (~50 including tests and documentation), 9 SK packages removed (InMemory connector retained), 3 new infrastructure components (parser, renderer, factory), 6 prompt templates renamed/relocated.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Semantic Model Integrity | PASS | Semantic model format unchanged; no re-enrichment needed |
| II. AI Integration via Semantic Kernel | JUSTIFIED VIOLATION | Entire feature replaces SK with M.E.AI — Constitution amendment (T053) included in plan |
| III. Repository Pattern for Persistence | PASS | Repository layer unchanged |
| IV. Project-Based Workflow | PASS | `settings.json` backward-compatible |
| V. Test-First Development | PASS | All tasks include test-first ordering (NON-NEGOTIABLE) |
| VI. CLI-First Interface | PASS | Zero CLI changes |
| VII. DI & Configuration | PASS | New services registered as singletons in `HostBuilderExtensions` |

**Post-Design Re-Check**: All gates PASS. Principle II violation is the migration itself — amendment tracked as T053.

## Project Structure

### Documentation (this feature)

```text
specs/001-migrate-agent-framework/
├── plan.md              # This file
├── research.md          # Phase 0: Technology research (9 decisions R1-R9)
├── data-model.md        # Phase 1: Entity changes (3 new, 3 deleted, 4 retained)
├── quickstart.md        # Phase 1: Migration quickstart guide
├── contracts/           # Phase 1: Interface contracts
│   ├── IChatClientFactory.cs
│   ├── IPromptTemplateParser.cs
│   └── ILiquidTemplateRenderer.cs
└── tasks.md             # Phase 2: 56 tasks across 10 phases
```

### Source Code (changes)

```text
src/GenAIDBExplorer/
├── GenAIDBExplorer.Core/
│   ├── ChatClients/                    # NEW: IChatClientFactory + ChatClientFactory
│   ├── PromptTemplates/                # NEW: Parser, Renderer, Models
│   │   ├── IPromptTemplateParser.cs
│   │   ├── PromptTemplateParser.cs
│   │   ├── ILiquidTemplateRenderer.cs
│   │   ├── LiquidTemplateRenderer.cs
│   │   ├── PromptTemplateDefinition.cs
│   │   ├── PromptTemplateMessage.cs
│   │   ├── PromptTemplateModelParameters.cs
│   │   └── *.prompt                    # Renamed from Prompty/*.prompty
│   ├── SemanticProviders/              # MODIFIED: Migrate to IChatClient
│   ├── DataDictionary/                 # MODIFIED: Migrate to IChatClient
│   ├── SemanticVectors/
│   │   ├── Embeddings/
│   │   │   ├── ChatClientEmbeddingGenerator.cs  # NEW
│   │   │   └── SemanticKernelEmbeddingGenerator.cs  # DELETED
│   │   ├── Indexing/
│   │   │   └── SkInMemoryVectorIndexWriter.cs  # RETAINED (no SK core dep)
│   │   └── Search/
│   │       ├── SkInMemoryVectorSearchService.cs  # RETAINED (no SK core dep)
│   │       ├── InMemoryVectorStoreAdapter.cs     # RETAINED (no SK core dep)
│   │       └── IVectorStoreAdapter.cs            # RETAINED (no SK core dep)
│   └── SemanticKernel/                 # DELETED entirely
├── GenAIDBExplorer.Console/
│   └── Extensions/HostBuilderExtensions.cs  # MODIFIED: New DI registrations
└── Tests/Unit/GenAIDBExplorer.Core.Test/
    ├── ChatClients/                    # NEW test files
    ├── PromptTemplates/                # NEW test files
    ├── SemanticProviders/              # MODIFIED test files
    └── DataDictionary/                 # MODIFIED test files
```

**Structure Decision**: Single .NET solution structure retained. New code is added to existing Core and Console projects. No new projects created.

## Key Design Decisions

### D1: InMemory Vector Store — Keep `Microsoft.SemanticKernel.Connectors.InMemory`

**Decision**: Retain `Microsoft.SemanticKernel.Connectors.InMemory` as a standalone package dependency.

**Research Finding** (2026-02-21): The package has **zero dependency on Semantic Kernel core**. Its .NET 10 dependencies are exclusively:

- `Microsoft.Extensions.AI.Abstractions` >= 10.2.0
- `Microsoft.Extensions.DependencyInjection.Abstractions` >= 10.0.2
- `Microsoft.Extensions.VectorData.Abstractions` >= 10.0.0
- `System.Numerics.Tensors` >= 10.0.2

The NuGet README explicitly states: *"This package can be used with Semantic Kernel or independently and does not depend on any Semantic Kernel abstractions or core libraries."*

**Verified**: The `SkInMemoryVectorIndexWriter`, `SkInMemoryVectorSearchService`, `InMemoryVectorStoreAdapter`, and `IVectorStoreAdapter` source files import ONLY `Microsoft.SemanticKernel.Connectors.InMemory` — no SK core imports, no SKEXP pragmas. They will compile without SK core packages.

**Impact**: Simplifies migration significantly — no DI swap needed for vector store, no custom replacement implementations required, E2E vector tests unchanged.

**Alternatives rejected**:

- Custom `InMemoryVectorIndexWriter`/`InMemoryVectorSearchService` (81 lines, already in codebase): Functional but doesn't implement `Microsoft.Extensions.VectorData` interfaces. Would require maintaining custom code.
- No other Microsoft first-party InMemory VectorData package exists.

### D2: Package Removal Strategy

Remove 9 SK packages, retain 1:

| Package | Action |
|---------|--------|
| `Microsoft.SemanticKernel` | REMOVE |
| `Microsoft.SemanticKernel.Abstractions` | REMOVE |
| `Microsoft.SemanticKernel.Connectors.AzureOpenAI` | REMOVE |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | REMOVE |
| `Microsoft.SemanticKernel.Core` | REMOVE |
| `Microsoft.SemanticKernel.PromptTemplates.Handlebars` | REMOVE |
| `Microsoft.SemanticKernel.PromptTemplates.Liquid` | REMOVE |
| `Microsoft.SemanticKernel.Prompty` | REMOVE |
| `Microsoft.SemanticKernel.Yaml` | REMOVE |
| `Microsoft.SemanticKernel.Connectors.InMemory` | **RETAIN** |

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Constitution Principle II (AI via SK) | Entire feature IS replacing SK with M.E.AI | N/A — this is the goal of the feature |
| Retaining one SK-namespaced package | `Microsoft.SemanticKernel.Connectors.InMemory` has no SK core dep | Custom replacement doesn't implement VectorData interfaces; no alternative first-party package exists |
