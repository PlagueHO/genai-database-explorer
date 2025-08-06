---
title: Natural Language Query Provider Specification
version: 1.0
date_created: 2025-01-25
last_updated: 2025-01-25
owner: GenAI Database Explorer Team
tags: [data, natural-language, query, semantic-kernel, ai, sql-generation]
---

# Introduction

This specification defines the Natural Language Query Provider component that enables users to query database semantic models using natural language. The provider leverages Semantic Kernel and multiple AI prompts to convert natural language questions into SQL queries, execute them against the database, and provide intelligent explanations of results.

## 1. Purpose & Scope

This specification defines the requirements, interfaces, and behavior for the Natural Language Query Provider component within the GenAI Database Explorer system. The provider serves as the core engine for converting natural language questions into executable SQL queries and providing meaningful responses.

**Target Audience**: Developers implementing natural language query capabilities, AI practitioners, and database administrators.

**Scope**: This specification covers the Natural Language Query Provider interface, implementation patterns, AI prompt integration, and query execution workflows. It does not cover the CLI interface or web interface implementations that consume this provider.

## 2. Definitions

- **Natural Language Query**: A question or request expressed in everyday language that needs to be converted to SQL
- **Semantic Model**: AI-enhanced database schema representation containing tables, views, stored procedures, and their relationships
- **Query Provider**: Service that processes natural language queries and generates SQL responses
- **Prompty File**: AI prompt template file used by Semantic Kernel for structured AI interactions
- **Query Result**: Structured response containing SQL query, execution results, and explanations
- **SQL Generation**: Process of converting natural language to valid SQL statements
- **Result Explanation**: AI-generated natural language explanation of query results

## 3. Requirements, Constraints & Guidelines

### Functional Requirements

- **REQ-001**: The provider SHALL convert natural language questions into valid SQL queries using the semantic model
- **REQ-002**: The provider SHALL execute generated SQL queries against the target database
- **REQ-003**: The provider SHALL provide natural language explanations of query results
- **REQ-004**: The provider SHALL support both data retrieval queries and schema explanation queries
- **REQ-005**: The provider SHALL validate generated SQL queries for safety before execution
- **REQ-006**: The provider SHALL handle multiple database schema types (tables, views, stored procedures)
- **REQ-007**: The provider SHALL provide detailed error messages for failed query generation or execution

### Security Requirements

- **SEC-001**: Generated SQL queries SHALL be validated to prevent SQL injection attacks
- **SEC-002**: The provider SHALL only allow SELECT statements and safe stored procedure calls
- **SEC-003**: Database connection credentials SHALL be handled securely without logging
- **SEC-004**: Query results SHALL not expose sensitive system metadata
- **SEC-005**: AI prompts SHALL not include sensitive connection or authentication information

### Performance Constraints

- **CON-001**: SQL query generation SHALL complete within 30 seconds for typical questions
- **CON-002**: Query execution SHALL respect database timeout configurations
- **CON-003**: Result explanation generation SHALL complete within 15 seconds
- **CON-004**: The provider SHALL handle concurrent query requests efficiently
- **CON-005**: Memory usage SHALL be optimized for large result sets

### Implementation Guidelines

- **GUD-001**: Use Semantic Kernel framework for AI prompt orchestration
- **GUD-002**: Implement async/await patterns for all AI and database operations
- **GUD-003**: Use structured logging for debugging and monitoring query generation
- **GUD-004**: Follow dependency injection patterns for testability
- **GUD-005**: Use Prompty files for AI prompt templates and versioning

### Design Patterns

- **PAT-001**: Implement the Provider pattern for query processing abstraction
- **PAT-002**: Use the Strategy pattern for different query types (data vs schema)
- **PAT-003**: Apply the Template Method pattern for query processing workflows
- **PAT-004**: Use the Factory pattern for creating query-specific components

## 4. Interfaces & Data Contracts

### Core Provider Interface

```csharp
public interface INaturalLanguageQueryProvider
{
    Task<QueryResult> ProcessQuestionAsync(SemanticModel semanticModel, string question);
    Task<SqlGenerationResult> GenerateSqlQueryAsync(SemanticModel semanticModel, string question);
    Task<SchemaExplanationResult> ExplainSchemaAsync(SemanticModel semanticModel, string question);
    Task<QueryExecutionResult> ExecuteQueryAsync(string sqlQuery);
    Task<string> ExplainResultsAsync(QueryExecutionResult results, string originalQuestion);
    Task<bool> ValidateQuerySafetyAsync(string sqlQuery);
}
```

### Data Contracts

#### QueryResult

```csharp
public class QueryResult
{
    public string OriginalQuestion { get; set; }
    public QueryType QueryType { get; set; }
    public SqlGenerationResult SqlGeneration { get; set; }
    public QueryExecutionResult ExecutionResult { get; set; }
    public string Explanation { get; set; }
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
```

#### SqlGenerationResult

```csharp
public class SqlGenerationResult
{
    public string GeneratedSql { get; set; }
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; }
    public QueryType QueryType { get; set; }
    public List<string> ReferencedTables { get; set; }
    public List<string> SelectedColumns { get; set; }
    public string Explanation { get; set; }
}
```

#### QueryExecutionResult

```csharp
public class QueryExecutionResult
{
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; }
    public DataTable Results { get; set; }
    public int RowCount { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string ExecutedQuery { get; set; }
}
```

#### SchemaExplanationResult

```csharp
public class SchemaExplanationResult
{
    public string Question { get; set; }
    public string Explanation { get; set; }
    public List<string> RelevantObjects { get; set; }
    public List<RelatedEntity> RelatedEntities { get; set; }
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; }
}
```

#### QueryType Enumeration

```csharp
public enum QueryType
{
    DataRetrieval,
    SchemaExplanation,
    AggregationQuery,
    JoinQuery,
    FilteredQuery,
    StoredProcedureCall,
    Unknown
}
```

### Method Specifications

#### ProcessQuestionAsync

**Purpose**: Main entry point for processing natural language questions

**Parameters**:
- `semanticModel`: The semantic model to query against
- `question`: Natural language question from the user

**Returns**: Complete `QueryResult` with SQL generation, execution, and explanation

**Behavior**: Orchestrates the complete workflow from question analysis to result explanation

#### GenerateSqlQueryAsync

**Purpose**: Convert natural language question to SQL query

**Parameters**:
- `semanticModel`: The semantic model containing database schema information
- `question`: Natural language question to convert

**Returns**: `SqlGenerationResult` containing generated SQL and metadata

**Behavior**: Uses AI prompts to analyze the question and generate appropriate SQL

#### ExecuteQueryAsync

**Purpose**: Execute validated SQL query against the database

**Parameters**:
- `sqlQuery`: Valid SQL query string to execute

**Returns**: `QueryExecutionResult` with execution results and metadata

**Behavior**: Executes query with proper error handling and result formatting

## 5. Acceptance Criteria

### Query Generation

- **AC-001**: Given a simple data retrieval question, When processed by the provider, Then a valid SELECT statement SHALL be generated
- **AC-002**: Given a question about table relationships, When processed, Then appropriate JOIN statements SHALL be generated
- **AC-003**: Given a question with filtering criteria, When processed, Then appropriate WHERE clauses SHALL be generated
- **AC-004**: Given a question requiring aggregation, When processed, Then appropriate GROUP BY and aggregate functions SHALL be generated
- **AC-005**: Given an ambiguous question, When processed, Then the provider SHALL request clarification or make reasonable assumptions

### Query Validation

- **AC-006**: Given a generated SQL query, When validated for safety, Then SQL injection patterns SHALL be detected and rejected
- **AC-007**: Given a DELETE or UPDATE statement, When validated, Then it SHALL be rejected as unsafe
- **AC-008**: Given a query accessing system tables, When validated, Then it SHALL be restricted appropriately
- **AC-009**: Given a valid SELECT statement, When validated, Then it SHALL be approved for execution

### Query Execution

- **AC-010**: Given a valid SQL query, When executed, Then results SHALL be returned in structured format
- **AC-011**: Given a query that times out, When executed, Then a timeout error SHALL be returned with appropriate message
- **AC-012**: Given a query with syntax errors, When executed, Then detailed error information SHALL be provided
- **AC-013**: Given a large result set, When executed, Then results SHALL be handled efficiently without memory issues

### Result Explanation

- **AC-014**: Given query results, When explanation is requested, Then a natural language summary SHALL be generated
- **AC-015**: Given empty results, When explained, Then appropriate reasoning SHALL be provided
- **AC-016**: Given complex query results, When explained, Then key insights and patterns SHALL be highlighted
- **AC-017**: Given error results, When explained, Then actionable suggestions SHALL be provided

### Schema Understanding

- **AC-018**: Given a question about table structure, When processed, Then accurate schema information SHALL be provided
- **AC-019**: Given a question about relationships, When processed, Then foreign key relationships SHALL be explained
- **AC-020**: Given a question about data types, When processed, Then column type information SHALL be provided accurately

## 6. Test Automation Strategy

### Test Levels

- **Unit Tests**: Individual method testing with mocked dependencies
- **Integration Tests**: End-to-end query processing with test databases
- **AI Model Tests**: Prompt effectiveness and response quality validation
- **Performance Tests**: Query generation and execution performance benchmarks

### Test Frameworks

- **MSTest**: Primary test framework for .NET unit tests
- **FluentAssertions**: Assertion library for readable test expectations
- **Moq**: Mocking framework for database and AI service dependencies
- **TestContainers**: Containerized test databases for integration tests

### Test Data Management

- **Sample Databases**: Well-defined test schemas with known data patterns
- **Question Libraries**: Curated sets of natural language questions with expected results
- **Mock Semantic Models**: Test semantic models with various complexity levels
- **AI Response Mocks**: Cached AI responses for consistent unit testing

### CI/CD Integration

- **Automated Testing**: All tests run on every pull request
- **Performance Benchmarks**: Automated performance regression detection
- **AI Model Validation**: Periodic validation of prompt effectiveness
- **Database Compatibility**: Testing against multiple database versions

### Test Coverage Requirements

- **Unit Tests**: Minimum 90% code coverage for core provider logic
- **Integration Tests**: Coverage of all major query types and error scenarios
- **AI Prompt Tests**: Validation of all Prompty files and expected responses
- **Performance Tests**: Baseline metrics for query generation and execution times

## 7. Rationale & Context

### Technology Choices

**Semantic Kernel**: Chosen for its robust prompt orchestration capabilities and integration with various AI models. Provides structured approach to AI interactions with built-in retry logic and error handling.

**Prompty Files**: Enable version control of AI prompts, facilitate A/B testing of prompt effectiveness, and provide clear separation between code and prompt engineering.

**DataTable for Results**: Provides structured, strongly-typed result handling with built-in serialization support and compatibility with various output formats.

**Async/Await Pattern**: Essential for AI service calls and database operations that involve network I/O and potentially long-running operations.

### Query Processing Strategy

The provider uses a multi-stage approach:

1. **Question Analysis**: Classify the question type and extract key entities
2. **SQL Generation**: Use semantic model context to generate appropriate SQL
3. **Validation**: Ensure generated SQL is safe and syntactically correct
4. **Execution**: Run validated SQL against the database with proper error handling
5. **Explanation**: Generate natural language explanation of results

### Error Handling Philosophy

The provider follows graceful degradation principles:
- Invalid questions result in helpful error messages rather than exceptions
- Failed SQL generation provides debugging information
- Database errors are translated to user-friendly messages
- Partial results are better than complete failures

### AI Prompt Strategy

Different prompt templates are used for different query types:
- **Data Retrieval**: Focus on SELECT statement generation
- **Schema Explanation**: Emphasize relationships and structure
- **Aggregation**: Handle GROUP BY and mathematical operations
- **Complex Joins**: Navigate multi-table relationships

## 8. Dependencies & External Integrations

### External Systems

- **EXT-001**: Target Database - SQL Server, PostgreSQL, or other relational databases for query execution
- **EXT-002**: AI Language Models - Azure OpenAI, OpenAI API, or other compatible services

### Third-Party Services

- **SVC-001**: Semantic Kernel Framework - AI orchestration and prompt management
- **SVC-002**: Azure OpenAI Service - Primary AI model provider for natural language processing
- **SVC-003**: Database Connection Providers - ADO.NET or Entity Framework for database connectivity

### Infrastructure Dependencies

- **INF-001**: .NET 9 Runtime - Target framework for application execution
- **INF-002**: Database Connectivity - Network access to target databases
- **INF-003**: AI Service Connectivity - Network access to AI model endpoints

### Data Dependencies

- **DAT-001**: Semantic Model - Rich database schema representation with AI enhancements
- **DAT-002**: AI Model Training Data - Underlying training data affects query generation quality
- **DAT-003**: Database Schema - Current schema structure for accurate SQL generation

### Technology Platform Dependencies

- **PLT-001**: Microsoft.SemanticKernel - Core AI orchestration framework
- **PLT-002**: System.Data.Common - Database abstraction and connectivity
- **PLT-003**: Microsoft.Extensions.Logging - Structured logging infrastructure
- **PLT-004**: Newtonsoft.Json - JSON serialization for AI interactions

### Compliance Dependencies

- **COM-001**: Data Privacy Regulations - Ensure query results don't expose sensitive data
- **COM-002**: Database Security Policies - Respect database access controls and permissions

## 9. Examples & Edge Cases

### Basic Query Generation

```csharp
// Simple data retrieval
var question = "Show me all customers from California";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Expected SQL: SELECT * FROM Customers WHERE State = 'CA'

// Aggregation query
var question = "How many orders were placed last month?";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Expected SQL: SELECT COUNT(*) FROM Orders WHERE OrderDate >= DATEADD(month, -1, GETDATE())

// Join query
var question = "List customers and their recent orders";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Expected SQL: SELECT c.Name, o.OrderDate, o.Total FROM Customers c 
//               JOIN Orders o ON c.CustomerID = o.CustomerID 
//               WHERE o.OrderDate >= DATEADD(day, -30, GETDATE())
```

### Schema Explanation Examples

```csharp
// Table structure question
var question = "What columns does the Products table have?";
var result = await queryProvider.ExplainSchemaAsync(semanticModel, question);
// Expected: Detailed explanation of Products table columns, types, and relationships

// Relationship question
var question = "How are customers related to orders?";
var result = await queryProvider.ExplainSchemaAsync(semanticModel, question);
// Expected: Explanation of foreign key relationships between Customers and Orders tables
```

### Edge Cases

```csharp
// Ambiguous question
var question = "Show me the data";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Expected: Request for clarification or reasonable default query

// Invalid SQL injection attempt
var question = "Show customers; DROP TABLE Users;";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Expected: Validation error, query rejected

// Complex multi-table query
var question = "Show the top 10 products by revenue from customers in New York who ordered in the last quarter";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Expected: Complex JOIN with aggregation, filtering, and ordering

// Empty results
var question = "Show customers from Antarctica";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Expected: Empty result set with explanation of why no results were found

// Database connection failure
var result = await queryProvider.ExecuteQueryAsync("SELECT * FROM Products");
// Expected: Connection error with retry suggestions and troubleshooting information
```

### Prompty File Examples

```yaml
# generate_sql_query.prompty
---
name: Generate SQL Query
description: Convert natural language questions to SQL queries using semantic model context
authors:
  - GenAI Database Explorer Team
model:
  api: chat
  configuration:
    type: azure_openai
    connection: default
    model: gpt-4
sample:
  question: "Show me all customers from California"
  semantic_model: "{{semantic_model}}"
---

You are an expert SQL query generator. Given a semantic model and a natural language question, generate a valid SQL query.

## Semantic Model Context
{{semantic_model}}

## User Question
{{question}}

## Instructions
1. Analyze the question to understand the data requirements
2. Identify relevant tables and columns from the semantic model
3. Generate a valid SQL SELECT statement
4. Include appropriate JOINs if multiple tables are needed
5. Add WHERE clauses for any filtering requirements
6. Use proper SQL syntax and formatting

## Output Format
Return only the SQL query, properly formatted and ready for execution.
```

## 10. Validation Criteria

### Functional Validation

- All query types (data retrieval, aggregation, joins, schema explanation) work correctly
- Generated SQL queries are syntactically correct and executable
- Query validation correctly identifies and blocks unsafe queries
- Result explanations are accurate and helpful
- Error messages provide actionable guidance

### Performance Validation

- Query generation completes within 30 seconds for 95% of questions
- Database query execution respects timeout configurations
- Memory usage remains stable for large result sets
- Concurrent query processing performs efficiently
- AI service rate limits are handled gracefully

### Security Validation

- SQL injection attempts are detected and blocked
- Only safe query types (SELECT, safe stored procedures) are allowed
- Database credentials are never logged or exposed
- Query results don't expose system metadata
- AI prompts don't contain sensitive information

### Quality Validation

- Generated SQL queries produce expected results for test questions
- Query explanations are clear and accurate
- Schema explanations correctly describe database relationships
- Error handling provides meaningful feedback
- AI responses are consistent and reliable

## 11. Related Specifications / Further Reading

- [GenAI Database Explorer Console CLI Specification](spec-app-console-cli-interface.md)
- [Semantic Model Storage Specification](../docs/technical/SEMANTIC_MODEL_STORAGE.md)
- [Semantic Model Documentation](../docs/components/semantic-model-documentation.md)
- [Microsoft Semantic Kernel Documentation](https://docs.microsoft.com/en-us/semantic-kernel/)
- [Prompty Documentation](https://github.com/microsoft/prompty)
- [SQL Security Best Practices](https://docs.microsoft.com/en-us/sql/relational-databases/security/sql-security-overview)
