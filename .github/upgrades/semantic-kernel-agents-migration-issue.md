# GitHub Issue: Migrate to Semantic Kernel Agents API

**Use this content to create a new issue using the chore_request.yml template**

---

## Area
**Code Quality / Refactoring**

---

## Motivation

The GenAI Database Explorer currently uses basic Semantic Kernel patterns for AI orchestration. The Semantic Kernel framework has evolved to include a dedicated **Agents API** (`Microsoft.SemanticKernel.Agents`) that provides:

1. **Multi-agent orchestration** - Ability to coordinate multiple specialized agents
2. **Agent conversations** - Structured multi-turn interactions between agents and users
3. **Built-in agent patterns** - ChatCompletionAgent, OpenAIAssistantAgent, etc.
4. **Better separation of concerns** - Agents encapsulate behavior, kernel manages execution
5. **Future-proof architecture** - Aligns with Microsoft's AI agent strategy

### Background

During the .NET 10 upgrade (completed in branch `upgrade-to-NET10`), we identified that the Semantic Kernel Agents API is the recommended evolution path for our AI orchestration code. This migration was **intentionally deferred** to be addressed in a separate, focused effort.

**Important**: This migration refers to **Semantic Kernel Agents** (within the SK framework), NOT the new standalone Microsoft Agent Framework (github.com/Microsoft/agent-framework) which is incompatible with our Prompty-based architecture.

### Current State

- Using `Kernel.InvokeAsync()` pattern directly
- 6 `.prompty` template files for prompt engineering
- Liquid template engine integration
- Manual orchestration of AI calls
- Located primarily in `SemanticProviders/SemanticDescriptionProvider.cs`

---

## Task Description

Migrate the GenAI Database Explorer solution from basic Semantic Kernel patterns to the **Semantic Kernel Agents API** while maintaining full compatibility with existing Prompty files.

### Scope

1. **Analyze Current Usage**
   - Inventory all Semantic Kernel invocations in the codebase
   - Document current prompt patterns and orchestration logic
   - Identify candidates for agent-based refactoring

2. **Design Agent Architecture**
   - Define agent roles (e.g., TableDescriptionAgent, ViewDescriptionAgent, etc.)
   - Plan agent interaction patterns
   - Design conversation flows if applicable
   - Maintain compatibility with existing Prompty files

3. **Implementation**
   - Add `Microsoft.SemanticKernel.Agents` package (currently experimental)
   - Refactor `SemanticDescriptionProvider` to use agent-based patterns
   - Create agent classes for different semantic model entities
   - Migrate from `Kernel.InvokeAsync()` to agent invocation patterns
   - Convert `.prompty` files into agent-compatible functions

4. **Validation**
   - Ensure all 6 `.prompty` files continue to work
   - Verify no changes to generated semantic descriptions
   - Confirm all 511 unit tests pass
   - Add new tests for agent-specific functionality

### Key Constraints

- ? **MUST maintain compatibility with existing `.prompty` files**
- ? **MUST NOT migrate to standalone Microsoft Agent Framework**
- ? Use `Microsoft.SemanticKernel.Agents` namespace
- ? Preserve existing Liquid templating functionality
- ? Keep all Kernel functions and plugins working

### Files Likely to Change

- `SemanticProviders/SemanticDescriptionProvider.cs` - Primary refactoring target
- `SemanticKernel/ISemanticKernelFactory.cs` - May need agent factory methods
- New agent classes to be created
- Unit tests in `GenAIDBExplorer.Core.Test`

### Reference Materials

- Semantic Kernel Agents Documentation: https://learn.microsoft.com/en-us/semantic-kernel/agents/
- Semantic Kernel GitHub: https://github.com/microsoft/semantic-kernel
- Prompty Documentation: https://github.com/microsoft/prompty
- .NET 10 Upgrade Report: `.github/upgrades/dotnet-upgrade-report.md`

---

## Acceptance Criteria

### Functional Requirements
- [ ] All Semantic Kernel operations migrated to Agents API
- [ ] All 6 `.prompty` files work correctly with agents
- [ ] Semantic descriptions generated are identical to current implementation
- [ ] All existing features continue to work (no regressions)

### Technical Requirements
- [ ] `Microsoft.SemanticKernel.Agents` package added
- [ ] Agent classes created with clear responsibilities
- [ ] Proper agent lifecycle management (creation, disposal)
- [ ] Thread-safe agent operations maintained
- [ ] Parallel processing capabilities preserved

### Quality Requirements
- [ ] All 511 unit tests pass
- [ ] New unit tests added for agent functionality (min. 80% coverage)
- [ ] No build warnings or errors
- [ ] Code follows existing patterns and conventions
- [ ] XML documentation comments on all public agent APIs

### Documentation Requirements
- [ ] Update relevant code comments
- [ ] Create migration guide documenting changes
- [ ] Update README if necessary
- [ ] Document agent architecture decisions

---

## Impact / Risk

### Low Risk
- **Backward Compatibility**: Semantic Kernel Agents is an evolution of existing patterns, not a replacement
- **Prompty Files**: Fully compatible, no changes required to `.prompty` files
- **Test Coverage**: Extensive existing tests will catch regressions

### Medium Risk
- **Experimental API**: `Microsoft.SemanticKernel.Agents` is marked experimental (SKEXP)
  - Mitigation: API is stable and recommended by Microsoft for production use
  - Monitor for breaking changes in future SK releases

### Potential Side Effects
- **Performance**: Agent-based patterns may have slightly different performance characteristics
  - Requires baseline performance testing before/after
  
- **Error Handling**: Agent invocation patterns may surface errors differently
  - Review and update error handling as needed

### Breaking Changes
- **None expected**: This is an internal refactoring
- Public API surface should remain unchanged
- Existing integrations should continue to work

---

## Additional Context

### Related Work
- **Completed**: .NET 10 upgrade in branch `upgrade-to-NET10` (ready to merge)
- **Confirmed**: Newtonsoft.Json ? System.Text.Json already complete
- **Future**: This Semantic Kernel Agents migration

### Branch Strategy
1. Ensure `upgrade-to-NET10` is merged to `main` first
2. Create new branch `feature/semantic-kernel-agents` from updated `main`
3. Complete migration work in feature branch
4. Create PR for review when complete

### Two Agent Framework Options (Analysis)

**Option A: Semantic Kernel Agents** ? **RECOMMENDED - This Issue**
- Part of Semantic Kernel framework
- Uses `Microsoft.SemanticKernel.Agents` namespace
- ? Fully compatible with Prompty files
- ? Maintains all existing patterns
- ? Evolutionary upgrade

**Option B: Microsoft Agent Framework** ? **NOT THIS ISSUE**
- Standalone framework: https://github.com/Microsoft/agent-framework
- Uses `Microsoft.AI.Agents` namespace
- ? NOT compatible with Prompty/Semantic Kernel
- ? Would require complete rewrite
- ? Cannot reuse `.prompty` files
- Not suitable for our architecture

### Current Prompty File Inventory
1. `describe_semanticmodeltable.prompty` - Table semantic descriptions
2. `describe_semanticmodelview.prompty` - View semantic descriptions  
3. `describe_semanticmodelstoredprocedure.prompty` - Stored procedure semantic descriptions
4. `get_tables_from_view_definition.prompty` - Extract tables from view SQL
5. `get_tables_from_storedprocedure_definition.prompty` - Extract tables from stored procedure SQL
6. `get_table_from_data_dictionary_markdown.prompty` - Parse data dictionary

### Success Indicators
- ? All tests green
- ? No change in semantic description quality
- ? Code more maintainable with clear agent responsibilities
- ? Foundation for future multi-agent scenarios

### Estimated Effort
- Research & Design: 4-6 hours
- Implementation: 8-12 hours
- Testing & Validation: 4-6 hours
- Documentation: 2-3 hours
- **Total: 18-27 hours** (2-3 days of focused work)

---

## Labels to Apply
- `chore`
- `refactoring`
- `semantic-kernel`
- `technical-debt`
- `ai-orchestration`

---

## Priority Recommendation
**Medium Priority** - Should be completed in Q1 2025 but not blocking other work.

---

**Created from:** .NET 10 Modernization final report  
**Related Branch:** `upgrade-to-NET10` (to be merged first)  
**Target Branch:** `feature/semantic-kernel-agents` (to be created)