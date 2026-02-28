# Feature Specification: Query Model with Microsoft Agent Framework

**Feature Branch**: `005-query-model-agent`
**Created**: 2026-02-28
**Status**: Draft
**Input**: User description: "Add the query-model command to the Console application. This command will take a question as an input. The question will then be sent to a Microsoft Agent Framework `AIAgent` backed by Foundry Agent Service, configured with function tools for semantic model search. The Agent Framework manages the ReAct agent loop internally — the backend defines function tools as C# methods that are executed locally during the reasoning cycle. The semantic model search tools use vector embedding search to find relevant database entities. The agent iteratively calls tools and reasons over results until it has enough information to answer — or terminates due to configured limits (tokens, time, or response rounds). The core functionality will be centralized in GenAIDBExplorer.Core so it can be reused by the API and Web App."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ask a Natural Language Question About the Database (Priority: P1)

As a database analyst, I want to ask a natural language question about my database and receive an intelligent answer so that I can understand my database schema, relationships, and business logic without writing SQL or reading documentation.

**Why this priority**: This is the primary user-facing feature - the entire purpose of the query-model command. Without it, there is no value delivered. It exercises the full pipeline: CLI input, agent orchestration, semantic model search, and answer generation.

**Independent Test**: Run `query-model --project <path> --question "What tables store customer information?"` and verify the system returns a relevant, accurate answer that references appropriate semantic model entities.

**Acceptance Scenarios**:

1. **Given** an enriched semantic model with vector embeddings generated, **When** the user runs `query-model --project <path> --question "What tables contain customer data?"`, **Then** the system returns an answer that correctly identifies relevant tables (e.g., `SalesLT.Customer`, `SalesLT.CustomerAddress`) with descriptions.
1. **Given** an enriched semantic model, **When** the user asks "How are products and categories related?", **Then** the agent performs multiple searches to find both Product and ProductCategory entities and explains the relationship between them.
1. **Given** a question that requires understanding of stored procedures, **When** the user asks "What stored procedures handle order processing?", **Then** the agent searches for stored procedure entities and returns relevant matches with their descriptions.
1. **Given** a question with no relevant matches in the semantic model, **When** the agent cannot find sufficient information after searching, **Then** it returns a clear explanation of why the question could not be fully answered and what information was found.

---

### User Story 2 - Agent Uses ReAct Loop to Iteratively Search (Priority: P1)

As a user asking a complex question, I want the agent to iteratively search and reason about the semantic model so that I get comprehensive answers that consider multiple related entities rather than just the first match.

**Why this priority**: The ReAct loop is the core differentiator from a simple search. Without iterative reasoning, the system would just return raw vector search results. The agent's ability to call tools multiple times and synthesize findings is essential for answering non-trivial questions.

**Independent Test**: Ask a question that requires finding information across multiple entity types (tables, views, stored procedures) and verify the agent's ReAct loop produces multiple response rounds (function call cycles) before the agent composes its answer.

**Acceptance Scenarios**:

1. **Given** a complex question like "Explain the complete order management workflow", **When** the question is submitted to the Agent Framework via `RunStreamingAsync()`, **Then** the agent issues multiple function tool calls across successive response rounds (for tables, views, and stored procedures), the framework executes each locally via registered C# methods, and the agent synthesizes the results into a coherent answer.
1. **Given** the agent has received partial search results from a function call, **When** the results suggest related entities to explore, **Then** the agent issues follow-up `function_call` outputs in the next response round to gather additional context before answering.
1. **Given** the agent has reached the configured maximum number of response rounds, **When** it has not yet gathered sufficient information, **Then** the backend terminates the loop and returns the best answer from information gathered so far, along with an explanation of the limitation.
1. **Given** the agent has reached its token budget or time limit, **When** it cannot continue processing, **Then** the backend terminates the loop and returns whatever information has been gathered along with the termination reason.

---

### User Story 3 - Core Query Service is Reusable Across Interfaces (Priority: P2)

As a developer building the API and web frontend, I want the semantic model query capabilities to be centralized in the Core library so that the same agent-powered query functionality is available in the CLI, API, and web application without duplication.

**Why this priority**: Architectural reusability is critical for the planned API and web app. Without centralization in Core, the query logic would need to be duplicated or refactored later. This is a design constraint, not a user-facing feature, so it's P2.

**Independent Test**: Verify that the query service interface and implementation reside in `GenAIDBExplorer.Core`, and that the Console command handler delegates to it without containing any query logic itself.

**Acceptance Scenarios**:

1. **Given** the Core library exposes a query service interface, **When** the Console command handler processes a question, **Then** it delegates entirely to the Core service and only handles CLI-specific concerns (argument parsing, output formatting).
1. **Given** the Core query service is registered in the DI container, **When** a future API controller needs query functionality, **Then** it can inject the same service interface without referencing the Console project.
1. **Given** the Core query service, **When** it is invoked, **Then** it returns a structured result object that can be serialized for any presentation layer (CLI text, JSON API response, web UI).

---

### User Story 4 - Semantic Model Search via Vector Embeddings (Priority: P1)

As an agent processing a user question, I need access to a semantic model search tool that uses vector embeddings to find relevant database entities so that I can discover tables, views, and stored procedures that match the user's intent.

**Why this priority**: The semantic model search tool is what the agent calls during its ReAct loop. Without it, the agent has no way to discover relevant entities. This is the foundation that enables User Stories 1 and 2.

**Independent Test**: Invoke the semantic model search service directly with a text query and verify it returns ranked results from the vector index with entity metadata (type, schema, name, content, score).

**Acceptance Scenarios**:

1. **Given** vector embeddings have been generated for the semantic model, **When** the search service receives a text query, **Then** it generates an embedding for the query, performs vector similarity search, and returns the top-K most relevant entities ranked by score.
1. **Given** a search query, **When** results are returned, **Then** each result includes the entity type (table/view/stored procedure), schema name, entity name, content description, and similarity score.
1. **Given** the search service is called with a query that matches entities across multiple schemas, **When** results are returned, **Then** entities from all matching schemas are included in the results.
1. **Given** no vector embeddings have been generated, **When** the search service is invoked, **Then** a clear error message indicates that embeddings must be generated first.

---

### User Story 5 - Agent Termination and Safety Guardrails (Priority: P2)

As a system operator, I want the agent to have configurable limits on its execution so that it does not consume excessive resources or run indefinitely when processing difficult questions.

**Why this priority**: Safety guardrails are important for production use but are secondary to core functionality. Without guardrails, the system still works for well-formed questions - the risk is unbounded resource consumption on edge cases.

**Independent Test**: Configure a low iteration limit and ask a complex question; verify the agent terminates within the limit and reports the termination reason.

**Acceptance Scenarios**:

1. **Given** a configured maximum number of tool calls (e.g., 10), **When** the agent reaches this limit while processing a question, **Then** it stops searching and returns the best answer from information gathered so far, including a note about hitting the iteration limit.
1. **Given** a configured token budget, **When** the agent's cumulative token usage approaches the budget, **Then** it terminates and returns its current findings with a note about the token limit.
1. **Given** a configured time limit, **When** the agent has been processing for longer than the allowed duration, **Then** it terminates and returns its current findings with a note about the time limit.
1. **Given** the agent terminates early due to any guardrail, **When** it returns its response, **Then** the response includes the termination reason and a summary of what was found.

---

### Edge Cases

- What happens when the semantic model has no vector embeddings generated? The system returns a clear error directing the user to run `generate-vectors` first.
- What happens when the question is empty or nonsensical? The agent returns a message indicating it cannot process the input and suggests rephrasing.
- What happens when the vector search returns no results? The agent acknowledges that no relevant entities were found and provides a best-effort answer based on its general knowledge, or suggests alternative queries.
- What happens when the Foundry agent endpoint is unreachable? The system returns a clear error message indicating the AI service is unavailable and suggests checking configuration.
- What happens when the Foundry Agent Service returns an error mid-loop (e.g., HTTP 429, 500, or run expiration) after some search results have been gathered? The system returns partial results gathered so far plus the error as the termination reason, so the user gets whatever information was collected before the failure.
- What happens when the semantic model is not enriched (no descriptions)? The system still functions but results may be lower quality; the search operates on whatever content was indexed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept a natural language question via the `query-model` CLI command with `--question` and `--project` parameters.
- **FR-002**: System MUST use the Microsoft Agent Framework (`AIAgent`) with the Azure AI Project provider backed by Foundry Agent Service to orchestrate a ReAct (Reasoning + Acting) loop. The framework manages reasoning and tool call/result cycling internally; the backend defines function tools as C# methods.
- **FR-003**: The agent MUST be configured with separate function tools for each entity type: `searchTables`, `searchViews`, and `searchStoredProcedures`. Each tool is a C# method decorated with `[Description]` attributes and converted via `AIFunctionFactory.Create()`. The Agent Framework executes these tools locally during the ReAct loop, enabling the agent to reason about which entity types to search.
- **FR-004**: The backend MUST use `agent.RunStreamingAsync()` to execute the agent loop. Streaming is the primary (and only) interaction mode. The Agent Framework internally handles the function_call → execute → function_call_output → repeat cycle and Foundry Agent Service communication. The backend enforces external guardrails (time, tokens, response rounds) by monitoring the streamed `AgentResponseUpdate` items and cancelling when limits are reached.
- **FR-005**: The agent MUST synthesize information from multiple search results into a coherent, natural language answer.
- **FR-006**: The backend MUST enforce termination guardrails: maximum number of response rounds, token budget, and time limit. When any limit is reached, the loop MUST stop and return the best answer available from information gathered so far.
- **FR-007**: When the backend terminates the loop due to a guardrail, it MUST return the best answer possible from information gathered so far, along with the termination reason.
- **FR-008**: The core query service (agent orchestration, tool execution, and search) MUST be implemented in `GenAIDBExplorer.Core` so it can be reused by the API and web application.
- **FR-009**: The query service MUST return a structured result containing the answer text, the entities referenced, the number of response rounds performed, token usage, and any termination reason.
- **FR-010**: The semantic model search service MUST generate an embedding for the search query, perform vector similarity search, and return ranked results with entity metadata.
- **FR-011**: The search service MUST use the existing `IEmbeddingGenerator` and `IVectorSearchService` infrastructure for embedding generation and vector search.
- **FR-012**: The agent MUST be created using `AIProjectClient.CreateAIAgentAsync()` (or `AIProjectClient.Agents.CreateAgentVersion()` + `AsAIAgent()`) with the model deployment name, agent instructions (from prompt template), and function tools. The Agent Framework wraps the Foundry Agent Service backend.
- **FR-013**: The system MUST validate that vector embeddings have been generated before attempting to query, returning a clear error if they have not.
- **FR-014**: The CLI command MUST progressively display the agent's streamed answer tokens to the console as they arrive, followed by referenced entities and metadata about the query process (response rounds, token usage).
- **FR-016**: The agent MUST be created once during service initialization and cleaned up on shutdown/dispose by calling `AIProjectClient.Agents.DeleteAgent(agentName)`. The same agent is reused across all queries in a session.
- **FR-017**: The backend SHOULD support optional use of Foundry conversations (`/openai/v1/conversations`) for multi-turn question sessions in the future, but the initial implementation processes each question as an independent response chain. This is out of scope for the current iteration.

### Key Entities

- **Query Request**: Represents a user's natural language question along with the project context. Contains the question text and a reference to the project path.
- **Query Result**: The structured outcome of processing a question. Contains the answer text, referenced entities, response round count, token usage, termination reason (if any), and timing information.
- **Semantic Model Search Result**: A single entity match from vector search. Contains entity type, schema, name, content description, and similarity score.
- **Foundry Agent Version**: A versioned agent definition in Foundry Agent Service, created with `PromptAgentDefinition` containing the model, instructions, and function tool definitions. Wrapped by the Agent Framework's `AIAgent` abstraction. Managed per-session and cleaned up after use.
- **Response Round**: A single tool-call cycle within the agent's ReAct loop. Each round may produce text output, function call requests, or both. Multiple rounds form the iterative reasoning loop managed by the Agent Framework.
- **Function Call Item**: A function tool invocation requested by the agent during reasoning. The Agent Framework automatically executes the registered C# method and feeds the result back to the agent.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can ask a natural language question and receive a relevant answer within 30 seconds for simple questions and within 60 seconds for complex multi-entity questions.
- **SC-002**: The agent correctly identifies relevant database entities in its answer for at least 80% of questions about schemas, relationships, and business logic.
- **SC-003**: The agent's ReAct loop executes at least 2 response rounds (with function call resolution) for complex questions that span multiple entity types, demonstrating iterative reasoning rather than single-shot retrieval.
- **SC-004**: The query-model command provides the same quality of answers regardless of whether it is invoked from the CLI, API, or web application, demonstrating successful centralization.
- **SC-005**: The agent terminates gracefully within configured limits 100% of the time, never running indefinitely or exceeding resource budgets.
- **SC-006**: Token usage for a typical query stays within a predictable range, and usage statistics are included in every query result for cost monitoring.

## Clarifications

### Session 2026-02-28

- Q: Should the semantic model search be exposed as one unified tool with an entity-type filter, or as separate tools per entity type? → A: Separate tools per entity type (`searchTables`, `searchViews`, `searchStoredProcedures`). This gives the agent clearer affordances and improves multi-round reasoning.

- Q: Should the Foundry agent version be created once per application startup or once per individual query? → A: Per-startup. The agent version is created once on service initialization and deleted on shutdown/dispose. This avoids per-query latency and suits interactive CLI sessions and future API/web scenarios.
- Q: If the Foundry Agent Service returns an error mid-loop after some search results have been gathered, what should happen? → A: Return partial results gathered so far plus the error as the termination reason. The user gets whatever information was collected before the failure.
- Q: Should the agent interaction use streaming or non-streaming? → A: Streaming from the start. All agent interactions use `agent.RunStreamingAsync()` to enable progressive token delivery for real-time CLI output and future web UI SSE support.

## Assumptions

- Vector embeddings have already been generated for the semantic model entities using the existing `generate-vectors` command. The query-model command does not generate embeddings on demand.
- A Microsoft Foundry project is available and configured in project settings. The Foundry project endpoint and model deployment name are configured via the existing `settings.json` `FoundryModels` pattern.
- The Microsoft Agent Framework (`Microsoft.Agents.AI.OpenAI` prerelease NuGet package) is used for `AIAgent`, `AIFunctionFactory`, `AgentSession`, `RunStreamingAsync`, and `AgentResponseUpdate`. The `Azure.AI.Projects` and `Azure.AI.Projects.OpenAI` SDKs are used for `AIProjectClient` and agent version management.
- The existing `IVectorSearchService` and `IEmbeddingGenerator` implementations (in-memory, SK-based) are sufficient for the search tool — no new vector store backend is needed for this feature.
- The Agent Framework internally handles the ReAct loop (function_call → execute → function_call_output → repeat). The backend defines function tools as C# methods and provides them to the agent; the framework handles all tool invocation orchestration.
- Tool execution happens entirely client-side in the backend application. The Foundry Agent Service does not execute the search tools directly, though this architecture may evolve in the future to move some tools server-side.
- The default guardrail limits (maximum response rounds, token budget, time limit) will be configurable through project settings, with reasonable defaults (e.g., 10 rounds, 60-second timeout).
- Agent versions are created once per service initialization and reused across all queries in that session. The service implements `IDisposable`/`IAsyncDisposable` to clean up the agent version on shutdown. The agent definition (instructions, tool schemas) can be configured but the lifecycle is tied to the service lifetime.
- Standard session-based conversation context (follow-up questions, conversation history via Foundry conversations) is out of scope for this initial implementation. Each question is processed as an independent response chain.
