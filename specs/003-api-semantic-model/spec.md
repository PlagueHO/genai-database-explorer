# Feature Specification: REST API for Semantic Model Repository

**Feature Branch**: `003-api-semantic-model`
**Created**: 2026-02-23
**Status**: Draft
**Input**: User description: "Add a REST API that exposes the core operations of the Semantic Model Repository for use by a front end Web Application. It should leverage the GenAIDBExplorer.Core project to provide access to the repository. It will be configured to load a GenAI Database Explorer project that has already been initialized. This API will be enhanced with additional operational tasks (such as calling the operational tasks that the console app calls), but this will be added later as these tasks will need to be performed asynchronously, which will require more complex API operations than the CRUD required for the basic Semantic Model Repository."

## Clarifications

### Session 2026-02-23

- Q: How should the API handle model staleness when underlying files change while the API is running? → A: Cache at startup + explicit reload endpoint.
- Q: Should entity list endpoints support pagination? → A: Yes, server-side pagination with offset/limit parameters.
- Q: Should there be a health/readiness endpoint? → A: Yes, health endpoint reporting model-loaded readiness, integrated with Aspire AppHost.
- Q: What conflict resolution strategy for concurrent writes? → A: Last-write-wins.
- Q: What format should error responses follow? → A: RFC 9457 Problem Details.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrieve the Semantic Model (Priority: P1)

As a front-end application developer, I want to retrieve the full semantic model from a configured project so that I can display the database schema to users for exploration and understanding.

**Why this priority**: This is the foundational read operation. Without the ability to retrieve the semantic model, no other front-end interaction is possible. It delivers immediate value by making the model accessible outside the CLI.

**Independent Test**: Can be fully tested by making a request to the model endpoint and verifying the complete semantic model (with tables, views, and stored procedures) is returned. Delivers the core value of web-based model browsing.

**Acceptance Scenarios**:

1. **Given** the API is running with a configured and initialized project, **When** a client requests the semantic model summary, **Then** the response contains the model name, source, description, and counts of tables, views, and stored procedures.
1. **Given** the API is running with a configured project, **When** a client requests the semantic model and the model files do not exist at the configured path, **Then** the API returns an appropriate error indicating the model is not found.
1. **Given** the API is running, **When** a client requests the semantic model, **Then** the response is returned in a standard structured format suitable for consumption by web applications.

---

### User Story 2 - Browse Individual Tables (Priority: P1)

As a front-end application user, I want to list all tables in the semantic model and retrieve details of a specific table (including columns, indexes, descriptions, and semantic descriptions) so that I can explore the database structure in detail.

**Why this priority**: Tables are the most commonly accessed entities in a database semantic model. Providing granular access to tables and their metadata is essential for any meaningful exploration experience.

**Independent Test**: Can be tested by listing all tables and then retrieving a specific table by schema and name, verifying all column and index details are included.

**Acceptance Scenarios**:

1. **Given** a loaded semantic model with tables, **When** a client requests the list of tables, **Then** the response contains a paginated summary list of tables with their schema, name, and description, along with total count and pagination metadata.
1. **Given** a loaded semantic model, **When** a client requests a specific table by schema name and table name, **Then** the response contains the full table details including columns, indexes, descriptions, semantic descriptions, and additional information.
1. **Given** a loaded semantic model, **When** a client requests a table that does not exist in the model, **Then** the API returns a not-found response.

---

### User Story 3 - Browse Individual Views (Priority: P2)

As a front-end application user, I want to list all views in the semantic model and retrieve details of a specific view (including columns, definition, and semantic descriptions) so that I can understand the virtual tables available in the database.

**Why this priority**: Views are the second most commonly explored entity type. They provide important logical groupings and transformations that users need to understand.

**Independent Test**: Can be tested by listing all views and retrieving a specific view, verifying columns and definition are returned.

**Acceptance Scenarios**:

1. **Given** a loaded semantic model with views, **When** a client requests the list of views, **Then** the response contains a paginated summary list of views with their schema, name, and description, along with total count and pagination metadata.
1. **Given** a loaded semantic model, **When** a client requests a specific view by schema name and view name, **Then** the response contains the full view details including columns, definition, and semantic descriptions.
1. **Given** a loaded semantic model, **When** a client requests a view that does not exist, **Then** the API returns a not-found response.

---

### User Story 4 - Browse Individual Stored Procedures (Priority: P2)

As a front-end application user, I want to list all stored procedures in the semantic model and retrieve details of a specific stored procedure (including parameters, definition, and semantic descriptions) so that I can understand the programmatic operations available in the database.

**Why this priority**: Stored procedures complete the set of core database entity types. Including them provides a comprehensive view of the database.

**Independent Test**: Can be tested by listing all stored procedures and retrieving a specific one, verifying parameters and definition are returned.

**Acceptance Scenarios**:

1. **Given** a loaded semantic model with stored procedures, **When** a client requests the list of stored procedures, **Then** the response contains a paginated summary list of stored procedures with their schema, name, and description, along with total count and pagination metadata.
1. **Given** a loaded semantic model, **When** a client requests a specific stored procedure by schema name and procedure name, **Then** the response contains the full stored procedure details including parameters, definition, and semantic descriptions.
1. **Given** a loaded semantic model, **When** a client requests a stored procedure that does not exist, **Then** the API returns a not-found response.

---

### User Story 5 - Update Entity Descriptions (Priority: P3)

As a front-end application user, I want to update the description or semantic description of a table, view, or stored procedure so that I can manually refine or correct AI-generated descriptions through the web interface.

**Why this priority**: Write operations enable collaborative model curation. While read-only browsing provides substantial value, the ability to update descriptions turns the web application into a productive tool rather than just a viewer.

**Independent Test**: Can be tested by updating a table's description and then retrieving the table again to confirm the change was persisted.

**Acceptance Scenarios**:

1. **Given** a loaded semantic model with a specific table, **When** a client updates the description of that table, **Then** the updated description is saved and subsequent retrieval reflects the change.
1. **Given** a loaded semantic model, **When** a client updates the semantic description of a view, **Then** the change is persisted to the model and the updated timestamp is refreshed.
1. **Given** a loaded semantic model, **When** a client attempts to update an entity that does not exist, **Then** the API returns a not-found response.

---

### User Story 6 - View Project Configuration (Priority: P3)

As a front-end application user, I want to view the current project configuration (project name, database source, persistence strategy) so that I understand which project and database the API is serving.

**Why this priority**: Provides important context for the user but is not required for core model browsing functionality.

**Independent Test**: Can be tested by requesting the project information endpoint and verifying the project name, database source, and repository configuration are returned.

**Acceptance Scenarios**:

1. **Given** the API is running with a configured project, **When** a client requests project information, **Then** the response contains the project name, database source identifier, and the persistence strategy in use.

---

### Edge Cases

- What happens when the configured project path does not exist or is inaccessible? The API returns a clear startup error or a service-unavailable response.
- What happens when the semantic model file is corrupted or malformed? The API returns an error indicating the model could not be loaded, with a meaningful error message.
- What happens when a client sends a request with an empty or whitespace-only schema name or entity name? The API returns a validation error.
- What happens when concurrent clients attempt to update the same entity simultaneously? The API uses last-write-wins; the most recent update overwrites the previous. No ETag or locking is required for the initial version.
- What happens when the underlying persistence store (disk, Azure Blob, CosmosDB) is temporarily unavailable? The API returns an appropriate error without crashing.
- What happens when the model is reloaded while read requests are in-flight? The API completes in-flight reads against the previous cached model and switches to the new model atomically.
- What happens when a client requests a page beyond the total number of entities? The API returns an empty list with the correct total count.
- What happens when a PATCH request body has both description and semanticDescription as null? The API returns a 400 validation error; at least one field must be non-null to constitute a valid update.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The API MUST load a pre-initialized GenAI Database Explorer project from a configured project path at startup.
- **FR-002**: The API MUST expose an endpoint to retrieve a semantic model summary including its name, source, description, and counts of tables, views, and stored procedures.
- **FR-003**: The API MUST expose endpoints to list tables, views, and stored procedures with summary information (schema, name, description), supporting server-side pagination via offset and limit parameters.
- **FR-004**: The API MUST expose endpoints to retrieve a specific table, view, or stored procedure by schema name and entity name, returning full entity details.
- **FR-005**: The API MUST expose endpoints to update the description and semantic description of individual tables, views, and stored procedures.
- **FR-006**: The API MUST persist changes made through update endpoints using the configured persistence strategy of the project.
- **FR-007**: The API MUST expose an endpoint to retrieve project configuration information (project name, database source, persistence strategy).
- **FR-008**: The API MUST return error responses using the RFC 9457 Problem Details format (`type`, `title`, `status`, `detail`, `instance` fields) for not-found entities, validation failures, and server errors.
- **FR-009**: The API MUST validate all input parameters (schema names, entity names) and reject malformed or empty values.
- **FR-010**: The API MUST support cross-origin requests to allow consumption by web front-end applications.
- **FR-011**: The API MUST leverage the existing `GenAIDBExplorer.Core` project and its `ISemanticModelRepository` for all model operations.
- **FR-012**: The API MUST handle concurrent requests safely without data corruption, using a last-write-wins strategy for concurrent updates to the same entity.
- **FR-013**: The API MUST log all operations using structured logging consistent with the existing application logging patterns.
- **FR-014**: The API MUST cache the semantic model in memory at startup and serve all read requests from the cache.
- **FR-015**: The API MUST expose an explicit reload endpoint that reloads the semantic model from persistence on demand, allowing operators or front-end clients to refresh the cached model after external changes.
- **FR-016**: The API MUST expose a health/readiness endpoint that reports whether the semantic model has been successfully loaded and the API is ready to serve requests. This endpoint MUST be compatible with the Aspire AppHost health-check integration.

### Key Entities

- **Semantic Model**: The top-level domain object representing a database schema analysis. Contains a name, source, description, and collections of tables, views, and stored procedures.
- **Table**: A database table entity within the semantic model. Has a schema, name, description, semantic description, columns (with types, constraints, and relationships), and indexes.
- **View**: A database view entity. Has a schema, name, description, semantic description, SQL definition, and columns.
- **Stored Procedure**: A database stored procedure entity. Has a schema, name, description, semantic description, SQL definition, and parameters.
- **Column**: A structural element of a table or view. Has a name, type, constraints (primary key, nullable, identity, computed), and optional foreign key references.
- **Index**: A structural element of a table. Has a name, type, and constraint flags (unique, primary key).
- **Project Configuration**: The settings that define which database project the API is serving, including the project path and persistence strategy.

## Assumptions

- The GenAI Database Explorer project has already been initialized and the semantic model has been extracted (and optionally enriched) before the API is started.
- The API serves a single project at a time, configured at startup. Multi-project support is out of scope.
- Operational tasks (extract, enrich, generate vectors, query) are explicitly out of scope for this feature and will be added in a future enhancement requiring asynchronous task management.
- The API is intended for use by a single-page web application front-end running in a browser.
- Authentication and authorization are not required for the initial version; the API will run within a trusted network or development environment. Security can be layered on in a future iteration.
- The API will use the same dependency injection and service configuration patterns established in the existing Console application.
- The API caches the semantic model at startup and does not automatically detect external file changes. A dedicated reload endpoint is provided to refresh the model on demand.
- The API will be registered as a resource in the existing Aspire AppHost so that its health endpoint integrates with Aspire orchestration and dashboard.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A front-end application can retrieve and display the full semantic model within 3 seconds of making a request.
- **SC-002**: All entity types (tables, views, stored procedures) can be listed and individually retrieved through the API with correct and complete data matching what the CLI produces.
- **SC-003**: Updates to entity descriptions made through the API are persisted and survive an API restart.
- **SC-004**: The API returns meaningful, structured error responses for all error conditions (not found, validation failure, server error) that a front-end can programmatically handle.
- **SC-005**: The API handles 10 concurrent read requests without errors or data corruption.
- **SC-006**: 100% of the existing `GenAIDBExplorer.Core` repository and model functionality used by the API is covered by existing or new unit tests.
