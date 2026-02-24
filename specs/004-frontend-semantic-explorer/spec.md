# Feature Specification: Frontend Semantic Model Explorer

**Feature Branch**: `004-frontend-semantic-explorer`
**Created**: 2026-02-24
**Status**: Draft
**Input**: User description: "Create a frontend application that can be used to interact with the semantic model and perform basic tasks. It should will also provide a chat interface that in future (not yet) will be able to able to perform agentic tasks against the Semantic Model by calling a backend Agentic service leveraging Microsoft Agent Framework and AG-UI. This initial feature is just for exploring the semantic model repository and providing basic editing features."

## Clarifications

### Session 2026-02-24

- Q: Should the frontend be integrated into .NET Aspire AppHost or deployed standalone? → A: Integrated into Aspire AppHost — started alongside the backend via `aspire run`, using Aspire JavaScript framework integration.
- Q: Which navigation layout pattern should be used? → A: Left sidebar navigation with a main content area; chat as a collapsible right panel.
- Q: Which column-level fields should be editable? → A: Description and Semantic Description only; name, type, and constraints are read-only.
- Q: Should entity-level editing include the NotUsed flag? → A: Yes — Description, Semantic Description, NotUsed flag, and NotUsed reason are all editable.
- Q: What should be explicitly declared out of scope? → A: Natural language querying, AI-powered enrichment from UI, schema extraction from UI, and user authentication/authorization.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse Semantic Model Overview (Priority: P1)

As a database analyst, I want to open the frontend application and immediately see a high-level summary of the loaded semantic model so that I can quickly understand the scope and composition of the database being explored.

**Why this priority**: This is the entry point to all other features. Without a model overview, users cannot navigate to specific objects. It delivers immediate value by showing the database structure at a glance.

**Independent Test**: Can be fully tested by loading a semantic model project and verifying the dashboard displays the model name, source, description, and counts of tables, views, and stored procedures.

**Acceptance Scenarios**:

1. **Given** a semantic model project is configured and loaded, **When** the user opens the frontend application, **Then** the application displays the model name, source, description, and summary counts (number of tables, views, stored procedures).
1. **Given** the backend API is unreachable, **When** the user opens the application, **Then** a clear error message is displayed indicating the service is unavailable with guidance on how to resolve it.
1. **Given** a semantic model is loaded, **When** the user views the overview, **Then** the overview displays the persistence strategy in use (LocalDisk, AzureBlob, or CosmosDB).

---

### User Story 2 - Browse and Search Tables (Priority: P1)

As a database analyst, I want to browse the list of tables in the semantic model, search and filter them by name or schema, and view detailed information about any table including its columns, indexes, descriptions, and semantic descriptions.

**Why this priority**: Tables are the most commonly explored database objects and the primary use case for exploring a semantic model. This delivers core navigation value.

**Independent Test**: Can be tested by loading a model with tables and verifying the list displays correctly, search filters work, and clicking a table shows full details including columns and indexes.

**Acceptance Scenarios**:

1. **Given** a semantic model with tables is loaded, **When** the user navigates to the tables section, **Then** a paginated list of tables is displayed showing schema name, table name, and description summary.
1. **Given** a list of tables is displayed, **When** the user types a search term, **Then** the list filters to show only tables whose name, schema, or description contains the search term.
1. **Given** a list of tables is displayed, **When** the user selects a table, **Then** the detail view shows the table's schema, name, description, semantic description, last update date, columns (with types, descriptions, and semantic descriptions), and indexes.
1. **Given** a table has the NotUsed flag set, **When** the user views the table detail, **Then** the NotUsed status and reason are clearly indicated.

---

### User Story 3 - Browse and Search Views (Priority: P2)

As a database analyst, I want to browse the list of views in the semantic model, search and filter them, and view detailed information about any view including its columns, descriptions, and semantic descriptions.

**Why this priority**: Views are the second most commonly explored objects. This follows the same pattern as tables and extends coverage of the model.

**Independent Test**: Can be tested by loading a model with views and verifying list display, search filtering, and detail views with columns.

**Acceptance Scenarios**:

1. **Given** a semantic model with views is loaded, **When** the user navigates to the views section, **Then** a paginated list of views is displayed showing schema name, view name, and description summary.
1. **Given** a list of views is displayed, **When** the user types a search term, **Then** the list filters to show only views matching the search term.
1. **Given** a list of views is displayed, **When** the user selects a view, **Then** the detail view shows the view's schema, name, description, semantic description, columns, and related metadata.

---

### User Story 4 - Browse and Search Stored Procedures (Priority: P2)

As a database analyst, I want to browse stored procedures in the semantic model, search and filter them, and view detailed information about any stored procedure including its parameters, descriptions, and semantic descriptions.

**Why this priority**: Stored procedures complete the three core object types in the semantic model. This rounds out the browsing experience.

**Independent Test**: Can be tested by loading a model with stored procedures and verifying list display, search, and detail views.

**Acceptance Scenarios**:

1. **Given** a semantic model with stored procedures is loaded, **When** the user navigates to the stored procedures section, **Then** a paginated list of stored procedures is displayed.
1. **Given** a list of stored procedures is displayed, **When** the user selects a stored procedure, **Then** the detail view shows its schema, name, description, semantic description, parameters, and related metadata.

---

### User Story 5 - Edit Entity Descriptions (Priority: P2)

As a database analyst, I want to edit the description and semantic description of tables, views, and stored procedures so that I can correct or enhance the model's documentation without using the CLI.

**Why this priority**: Editing is a core value-add over the CLI-only experience. It enables interactive refinement of the semantic model through the UI.

**Independent Test**: Can be tested by opening a table detail, editing its description, saving, and verifying the updated description persists when the page is reloaded.

**Acceptance Scenarios**:

1. **Given** a user is viewing a table detail, **When** the user clicks an edit action on the description field, **Then** the field becomes editable with save and cancel options.
1. **Given** a user has edited a description, **When** the user saves the change, **Then** the updated description is persisted via the backend API and the UI reflects the new value.
1. **Given** a user has edited a description, **When** the user cancels the edit, **Then** the original value is restored and no changes are persisted.
1. **Given** a user is editing a table, **When** the user modifies column-level descriptions, **Then** individual column descriptions can be updated and saved.
1. **Given** a save operation fails, **When** the backend returns an error, **Then** a clear error message is displayed and the unsaved changes are preserved in the UI for retry.

---

### User Story 6 - Chat Interface Placeholder (Priority: P3)

As a user, I want to see a chat interface panel in the application so that I am aware of the upcoming agentic capabilities, even though the chat is not yet functional.

**Why this priority**: The chat interface is a future capability, but including the UI placeholder establishes the application's vision and layout. It does not require backend integration in this phase.

**Independent Test**: Can be tested by verifying a chat panel is visible in the application layout with a clear message indicating the feature is coming soon.

**Acceptance Scenarios**:

1. **Given** the user opens the application, **When** the user opens the collapsible right chat panel, **Then** a chat-style interface is displayed with message input area and message display area.
1. **Given** the chat panel is displayed, **When** the user attempts to send a message, **Then** a friendly message is displayed indicating that agentic chat capabilities are coming in a future release.
1. **Given** the chat panel is displayed, **Then** the panel layout follows the AG-UI conversational interface pattern to ensure future compatibility.

---

### User Story 7 - Reload Semantic Model (Priority: P3)

As a database analyst, I want to reload the semantic model from the persistence store without restarting the application so that I can see the latest changes made by other tools or users.

**Why this priority**: Useful for collaborative workflows but not essential for the initial browsing experience.

**Independent Test**: Can be tested by modifying the model externally, clicking reload, and verifying the UI reflects the updated model.

**Acceptance Scenarios**:

1. **Given** a semantic model is loaded, **When** the user triggers a reload action, **Then** the model is refreshed from the persistence store and the UI updates to reflect any changes.
1. **Given** a reload is in progress, **When** the user views the UI, **Then** a loading indicator is displayed until the reload completes.

---

### Edge Cases

- What happens when the semantic model has no tables, views, or stored procedures? The UI should display an empty state with an informative message.
- What happens when a table has no columns? The detail view should indicate that no columns are defined.
- What happens when entity descriptions contain special characters or very long text? The UI should handle encoding correctly and provide appropriate text overflow handling (scrolling or truncation with expand).
- What happens when the user navigates to a table/view/stored procedure that no longer exists after a model reload? The UI should display a "not found" message and navigate back to the list.
- What happens when multiple users edit the same entity simultaneously? The last save wins; the UI should display the latest version on reload.
- What happens when the semantic model is very large (hundreds of tables)? Pagination must work correctly and the UI should remain responsive.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST display a dashboard with the semantic model summary including name, source, description, and entity counts.
- **FR-002**: The application MUST provide paginated, searchable lists for tables, views, and stored procedures.
- **FR-003**: The application MUST display detailed information for each entity including schema, name, description, semantic description, last update date, and NotUsed status.
- **FR-004**: The application MUST display column details for tables and views including column name, type, description, and semantic description.
- **FR-005**: The application MUST display index details for tables including index name, type, and columns.
- **FR-006**: The application MUST allow inline editing of entity Description, Semantic Description, NotUsed flag, and NotUsed reason for tables, views, and stored procedures.
- **FR-007**: The application MUST allow inline editing of column-level Description and Semantic Description for tables and views. Column name, type, and constraints MUST remain read-only.
- **FR-008**: The application MUST persist edited descriptions via the existing backend API.
- **FR-009**: The application MUST display clear error messages when the backend API is unreachable or returns errors.
- **FR-010**: The application MUST provide a model reload capability that refreshes data from the persistence store.
- **FR-011**: The application MUST include a chat interface panel with a placeholder indicating future agentic capabilities.
- **FR-012**: The application MUST use a left sidebar for navigation between the model overview, entity lists (tables, views, stored procedures), and entity details, with the chat panel as a collapsible right panel.
- **FR-013**: The application MUST display loading indicators during data fetch operations.
- **FR-014**: The application MUST display empty state messages when entity lists contain no items.
- **FR-015**: The application MUST communicate with the existing backend API endpoints (project, model, tables, views, stored procedures).

### Key Entities

- **Semantic Model**: The root object representing the entire database semantic model. Contains name, source, description, and collections of tables, views, and stored procedures.
- **Table**: A database table with schema, name, description, semantic description, columns, indexes, and NotUsed status.
- **View**: A database view with schema, name, description, semantic description, columns, and NotUsed status.
- **Stored Procedure**: A database stored procedure with schema, name, description, semantic description, parameters, and NotUsed status.
- **Column**: A column within a table or view with name, type, description, semantic description, and constraints. Only Description and Semantic Description are editable; all other attributes are read-only.
- **Index**: An index on a table with name, type, and associated columns.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view the semantic model summary within 3 seconds of opening the application.
- **SC-002**: Users can find any specific table, view, or stored procedure within 10 seconds using search.
- **SC-003**: Users can view complete details of any entity (including all columns) within 2 seconds of selecting it.
- **SC-004**: Users can edit and save an entity description in under 30 seconds.
- **SC-005**: 90% of users can navigate to a specific entity and understand its purpose on their first attempt without documentation.
- **SC-006**: The application displays meaningful error messages for all failure scenarios (API unavailable, save failures, not found).
- **SC-007**: The application remains responsive and usable with semantic models containing up to 500 tables.
- **SC-008**: All entity browsing and editing tasks can be completed without resorting to the CLI.

## Assumptions

### Out of Scope

The following capabilities are explicitly out of scope for this initial feature and will be addressed in future phases:

- **Natural language querying**: Querying the database via natural language through the frontend UI.
- **AI-powered enrichment from UI**: Triggering AI enrichment or re-enrichment of entity descriptions from the frontend.
- **Schema extraction from UI**: Extracting or re-extracting database schemas from the frontend.
- **User authentication/authorization**: No login, user accounts, or role-based access control.

### Included Assumptions

- The existing backend API (GenAIDBExplorer.Api) will be used as the data source. The API already provides endpoints for project info, model summary, and CRUD operations on tables, views, and stored procedures.
- The frontend will be a single-page application integrated into the .NET Aspire AppHost, started alongside the backend via `aspire run` using Aspire JavaScript framework integration.
- Authentication and authorization are not required for this initial version, as the application targets local development and exploration scenarios.
- The chat interface in this phase is UI-only with no backend integration; future phases will connect it to Microsoft Agent Framework via AG-UI protocol.
- The application layout should accommodate the future chat panel without requiring major restructuring.
- View and stored procedure PATCH endpoints will follow the same pattern as the existing table PATCH endpoint if not already implemented.
- The backend API will be extended as needed to support any missing endpoints identified during implementation planning.
