---
title: Natural Language Query Provider Specification
version: 3.0
date_created: 2025-01-25
last_updated: 2025-08-06
owner: GenAI Database Explorer Team
tags: [data, natural-language, query, semantic-kernel, ai, sql-generation, react, reasoning, semantic-model]
---

# Introduction

The Natural Language Query Provider enables users to ask comprehensive questions about database semantic models using natural language. It leverages Semantic Kernel and ReAct patterns to answer questions about database schemas, optimization, business logic, and data analysis.

## 1. Purpose & Scope

**Purpose**: Intelligent database consultant that processes natural language questions about semantic models, generates SQL, explains business logic, and provides database insights.

**Target Audience**: Developers, AI practitioners, database administrators, business analysts, data scientists.

**Key Capabilities**: SQL generation & optimization, schema analysis, business logic explanation, data analysis, multi-step reasoning (ReAct), educational support.

## 2. Definitions

- **Semantic Model**: AI-enhanced database schema with tables, procedures, relationships, and business context
- **ReAct Pattern**: Reasoning, Learning, and Acting approach for multi-step problem solving
- **Query Provider**: Service processing natural language questions about database models

## 3. Requirements

### Functional Requirements

- **REQ-001**: Answer any natural language question about semantic models (schema, data, business logic, optimization)
- **REQ-002**: Generate and execute valid SQL queries when appropriate
- **REQ-003**: Support multi-step reasoning using ReAct patterns
- **REQ-004**: Explain stored procedures, views, triggers, and database objects
- **REQ-005**: Provide query optimization suggestions and data insights
- **REQ-006**: Handle follow-up questions with conversation context
- **REQ-007**: Validate SQL queries for safety before execution

### Security & Performance

- **SEC-001**: Block SQL injection attempts, allow only SELECT/safe procedures
- **SEC-002**: Secure credential handling, no sensitive data in prompts/logs
- **CON-001**: Simple questions ≤15s, complex reasoning ≤60s
- **CON-002**: Limit ReAct steps to 10 to prevent infinite loops

### Implementation

- **GUD-001**: Use Semantic Kernel for AI orchestration via ISemanticKernelFactory and Prompty files for templates
- **GUD-002**: Implement async/await, dependency injection, structured logging with ILogger
- **GUD-003**: Apply Provider, Strategy, Chain of Responsibility patterns
- **GUD-004**: All natural language processing MUST use Large Language Models via SemanticKernelFactory.CreateSemanticKernel()
- **GUD-005**: Use Prompty files for all AI prompts, stored in Prompty folder following naming convention
- **GUD-006**: Track token usage and timing for cost monitoring and performance analysis
- **GUD-007**: Follow C# 14 best practices including primary constructors, file-scoped namespaces, and nullable reference types

## 4. Interfaces & Data Contracts

### Core Interface

```csharp
public interface INaturalLanguageQueryProvider
{
    Task<QuestionResult> ProcessQuestionAsync(SemanticModel semanticModel, string question, ConversationContext? context = null);
    Task<SqlGenerationResult> GenerateSqlQueryAsync(SemanticModel semanticModel, string question);
    Task<QueryExecutionResult> ExecuteQueryAsync(string sqlQuery);
    Task<bool> ValidateQuerySafetyAsync(string sqlQuery);
}

public interface INaturalLanguageQueryProviderFactory
{
    INaturalLanguageQueryProvider CreateProvider();
}
```

### Key Dependencies

```csharp
// Constructor dependencies following SemanticDescriptionProvider pattern
public class NaturalLanguageQueryProvider(
    IProject project,
    ISemanticKernelFactory semanticKernelFactory,
    ISchemaRepository schemaRepository,
    ILogger<NaturalLanguageQueryProvider> logger
) : INaturalLanguageQueryProvider
{
    // Implementation uses SemanticKernelFactory.CreateSemanticKernel()
    // for all LLM interactions and Prompty files for prompts
}
```

### Key Data Contracts

```csharp
public class QuestionResult
{
    public required string OriginalQuestion { get; set; }
    public QuestionType QuestionType { get; set; }
    public QuestionComplexity Complexity { get; set; }
    public required string Answer { get; set; }
    public List<string> ReasoningSteps { get; set; } = [];
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public ConversationContext? UpdatedContext { get; set; }
    public SemanticProcessResultItem? ProcessingMetrics { get; set; }
}

public class SqlGenerationResult
{
    public required string OriginalQuestion { get; set; }
    public required string GeneratedSql { get; set; }
    public string? Explanation { get; set; }
    public List<string>? OptimizationSuggestions { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public SemanticProcessResultItem? ProcessingMetrics { get; set; }
}

public class QueryExecutionResult
{
    public required string ExecutedSql { get; set; }
    public List<Dictionary<string, object?>>? ResultData { get; set; }
    public int? RowsAffected { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}

public class ConversationContext
{
    public required string ConversationId { get; set; }
    public List<string> History { get; set; } = [];
    public Dictionary<string, object> SharedContext { get; set; } = [];
    public DateTime LastUpdated { get; set; }
}

public enum QuestionType
{
    SqlGeneration, SchemaExplanation, BusinessLogicExplanation, 
    QueryOptimization, DataAnalysis, ComplexReasoning, Educational
}

public enum QuestionComplexity
{
    Simple,    // Single-step, direct answer
    Moderate,  // 2-3 reasoning steps
    Complex,   // Multiple steps, cross-referencing
    Advanced   // Deep reasoning, multiple models/contexts
}
```

## 5. Sample Questions by Complexity

### Simple (Direct Answers)

1. "What columns does the Customer table have?" - Schema retrieval
1. "Show me all orders from last month" - Basic SQL generation
1. "What data type is CustomerID?" - Column metadata
1. "How many records in Products table?" - Count aggregation

### Moderate (2-3 Steps)

1. "What does GetCustomerOrderHistory stored procedure do?" - Business logic
1. "How are customers related to orders?" - Relationship analysis
1. "Which tables have most foreign keys?" - Schema patterns
1. "Better way to write: SELECT * FROM Orders WHERE YEAR(OrderDate) = 2024?" - Basic optimization

### Complex (Multi-Step Analysis)

1. "Analyze data model for performance bottlenecks" - Schema analysis
1. "What happens if I delete a customer record?" - Impact analysis
1. "Compare approaches to find customers with no orders" - Query alternatives
1. "How does order fulfillment work via procedures/triggers?" - Process analysis

### Advanced (Deep Reasoning)

1. "Design strategy to identify churning customers by order patterns" - Predictive design
1. "What data quality issues exist and how to fix?" - Quality assessment
1. "Redesign product tables for multi-language support" - Architecture
1. "Analyze indexing strategy for common query patterns" - Performance review

### Educational/Comparative

1. "Explain clustered vs non-clustered indexes with examples" - Education
1. "Why choose stored procedure vs view for this use case?" - Design rationale

## 6. Acceptance Criteria

- **AC-001**: Given a simple schema question, When ProcessQuestionAsync is called, Then return accurate table/column details within 15 seconds
- **AC-002**: Given a natural language query request, When GenerateSqlQueryAsync is called, Then return valid SQL that produces expected results
- **AC-003**: Given an unsafe SQL query, When ValidateQuerySafetyAsync is called, Then return false and block execution
- **AC-004**: Given a multi-step complex question, When ProcessQuestionAsync is called, Then use ReAct pattern with logical reasoning steps
- **AC-005**: Given a follow-up question with context, When ProcessQuestionAsync is called with ConversationContext, Then use previous context appropriately
- **AC-006**: The system shall use SemanticKernelFactory.CreateSemanticKernel() for ALL natural language processing
- **AC-007**: The system shall use Prompty files for all AI prompts with proper error handling
- **AC-008**: The system shall track token usage and processing time for all LLM interactions
- **AC-009**: The system shall log all operations with structured logging including scopes

## 7. Test Automation Strategy

- **Test Levels**: Unit, Integration, End-to-End
- **Frameworks**: MSTest, FluentAssertions, Moq (for .NET applications)
- **Test Data Management**: Mock semantic models and conversation contexts for unit tests
- **CI/CD Integration**: Automated testing in GitHub Actions pipelines with test result reporting
- **Coverage Requirements**: Minimum 80% code coverage for core NaturalLanguageQueryProvider logic
- **Performance Testing**: Load testing for concurrent question processing and token usage monitoring
- **Integration Testing**: Test against real semantic models with actual LLM interactions
- **Security Testing**: Validate SQL injection prevention and safe query execution

## 8. Rationale & Context

This specification defines a comprehensive Natural Language Query Provider that leverages the existing GenAI Database Explorer infrastructure to provide intelligent database consultation capabilities.

### Design Decisions

**SemanticKernelFactory Integration**: Following the established pattern used in SemanticDescriptionProvider ensures consistency across the application and proper LLM service management. This approach provides centralized configuration for Azure OpenAI services and proper error handling.

**Prompty File Usage**: Using file-based prompts allows for version control of AI prompts, easier testing and refinement, and separation of prompt engineering from application code. This follows the same pattern established by other semantic providers in the system.

**Token Usage Tracking**: Implementing cost monitoring and performance tracking from the start ensures operational visibility and budget control for LLM usage, following the pattern established in SemanticDescriptionProvider.

**ReAct Pattern**: Complex database questions often require multi-step reasoning (Think→Learn→Act cycles). This approach enables the system to break down complex questions, gather relevant information, and provide comprehensive answers.

**Safety-First Approach**: Database query generation requires strict security validation to prevent SQL injection and unauthorized data access. The specification mandates validation of all generated SQL before execution.

**Conversation Context**: Supporting multi-turn conversations enables more natural interactions where users can ask follow-up questions and build upon previous queries without repeating context.

### Business Context

The Natural Language Query Provider serves as an intelligent database consultant that democratizes database access for non-technical users while providing advanced analysis capabilities for technical users. This addresses the common challenge of requiring SQL knowledge to extract insights from databases.

## 9. Usage Examples

### Basic Question Processing

```csharp
// Simple schema question
var question = "What columns does the Customer table have?";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Returns: Schema analysis with column details

// SQL generation
var question = "Show me all customers from California";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Returns: Generated SQL + execution results

// Business logic explanation
var question = "What does CalculateShippingCost stored procedure do?";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// Returns: Business purpose and parameter explanation
```

### Complex Multi-Step Reasoning

```csharp
// Performance analysis with ReAct
var question = "Analyze the data model for performance bottlenecks";
var result = await queryProvider.ProcessQuestionAsync(semanticModel, question);
// ReAct steps: Think → Learn → Act → Recommend optimizations

// Query optimization
var question = "Better way to find customers with no orders?";
var sql = "SELECT * FROM Customers WHERE CustomerID NOT IN (SELECT CustomerID FROM Orders)";
var result = await queryProvider.GenerateSqlQueryAsync(semanticModel, question);
// Returns: Multiple optimized alternatives with performance comparison
```

### Conversation Context

```csharp
// Initial question
var context = new ConversationContext { ConversationId = Guid.NewGuid().ToString() };
var question1 = "What tables relate to customer orders?";
var result1 = await queryProvider.ProcessQuestionAsync(semanticModel, question1, context);

// Follow-up using context
var question2 = "How to get total order value per customer?";
var result2 = await queryProvider.ProcessQuestionAsync(semanticModel, question2, context);
// Uses previous relationship context
```

## 10. Implementation Notes

### Technology Choices

- **Semantic Kernel**: AI orchestration, multi-step reasoning, conversation management
- **SemanticKernelFactory**: Creates configured Kernel instances with proper chat completion services
- **Prompty Files**: Version-controlled AI prompts for different question types
- **Conversation Context**: Handle follow-up questions and maintain analysis state
- **Token Usage Tracking**: Monitor costs and performance like SemanticDescriptionProvider

### Key Patterns

- **Question Classification**: Route to appropriate processing engine based on complexity
- **Multi-Modal Analysis**: Combine schema, business logic, and data insights
- **Graceful Degradation**: Provide partial results when complete analysis fails
- **Safety First**: Validate all SQL for injection attacks, allow only safe operations

### LLM Integration Requirements

- **REQ-LLM-001**: ALL natural language processing MUST use Large Language Models via `ISemanticKernelFactory.CreateSemanticKernel()`
- **REQ-LLM-002**: Use separate Prompty files for each question type (sql_generation.prompty, schema_analysis.prompty, etc.)
- **REQ-LLM-003**: Follow SemanticDescriptionProvider pattern for prompt execution with proper error handling
- **REQ-LLM-004**: Track token usage using `OpenAI.Chat.ChatTokenUsage` from result metadata
- **REQ-LLM-005**: Use structured logging with scope for each question processing session

### Prompty File Organization

```text
src/GenAIDBExplorer/GenAIDBExplorer.Core/Prompty/
├── query_sql_generation.prompty          # Generate SQL from natural language
├── query_schema_analysis.prompty         # Analyze database schema questions
├── query_business_logic.prompty          # Explain stored procedures/views
├── query_optimization.prompty            # Query optimization suggestions
├── query_classification.prompty          # Classify question type and complexity
├── query_safety_validation.prompty       # Validate SQL for safety
└── query_react_reasoning.prompty         # Multi-step ReAct reasoning
```

### Implementation Pattern

```csharp
// Follow SemanticDescriptionProvider pattern
private async Task<QuestionResult> ProcessWithPromptyAsync(string question, string promptyFile)
{
    var startTime = DateTime.UtcNow;
    var scope = $"NLQuery [{promptyFile}]";
    
    using (_logger.BeginScope(scope))
    {
        var promptExecutionSettings = new PromptExecutionSettings
        {
            ServiceId = "ChatCompletion"
        };

        var arguments = new KernelArguments(promptExecutionSettings)
        {
            { "question", question },
            { "semanticModel", semanticModel.ToYaml() },
            { "context", conversationContext }
        };

        var applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var promptyFilename = Path.Combine(applicationDirectory, "Prompty", promptyFile);

        var semanticKernel = _semanticKernelFactory.CreateSemanticKernel();
        var function = semanticKernel.CreateFunctionFromPromptyFile(promptyFilename);
        var result = await semanticKernel.InvokeAsync(function, arguments);

        var timeTaken = DateTime.UtcNow - startTime;
        var usage = result.Metadata?["Usage"] as OpenAI.Chat.ChatTokenUsage;
        
        return new QuestionResult
        {
            Answer = result.ToString(),
            ProcessingTime = timeTaken,
            ProcessingMetrics = new SemanticProcessResultItem(scope, "ChatCompletion", usage, timeTaken)
        };
    }
}
```

## 11. Dependencies & External Integrations

### External Systems

- **EXT-001**: Database System - Relational database for schema extraction and query execution
- **EXT-002**: Azure OpenAI Service - Large Language Model for natural language processing

### Third-Party Services

- **SVC-001**: Azure OpenAI - GPT models for question processing, SQL generation, and reasoning
- **SVC-002**: Microsoft Semantic Kernel - AI orchestration framework for prompt management

### Infrastructure Dependencies

- **INF-001**: SemanticKernelFactory - Creates configured Kernel instances with proper chat completion services
- **INF-002**: Project Settings - Configuration for OpenAI endpoints, API keys, and model deployments
- **INF-003**: Prompty File System - File-based prompt templates for different question types

### Data Dependencies

- **DAT-001**: Semantic Model - Enhanced database schema with business context and relationships
- **DAT-002**: Sample Data - Representative data samples for context-aware SQL generation
- **DAT-003**: Conversation Context - Session state for multi-turn conversations

### Technology Platform Dependencies

- **PLT-001**: .NET 10 - Target runtime with modern C# features
- **PLT-002**: Microsoft.SemanticKernel - AI orchestration and prompt execution
- **PLT-003**: System.Data.Common - Database connectivity for query execution

### Compliance Dependencies

- **COM-001**: SQL Injection Prevention - Security validation for all generated queries
- **COM-002**: Data Privacy - No sensitive data exposure in prompts or logs

## 12. Validation Criteria

### Functional

- All question types processed correctly (SQL, schema, business logic, optimization)
- Multi-step reasoning follows logical progression
- Conversation context maintained across interactions
- Security validation blocks unsafe queries
- All LLM interactions use SemanticKernelFactory.CreateSemanticKernel()
- Prompty files used for all AI prompts with proper error handling

### Performance

- Simple questions ≤15s, complex ≤60s
- Memory stable during reasoning chains
- Concurrent requests handled efficiently
- Token usage tracked and reported for cost monitoring

### Quality

- Generated SQL produces expected results
- Explanations are clear and accurate
- ReAct reasoning provides logical steps
- Error messages offer actionable guidance
- Processing metrics available for performance analysis

## 13. Related Specifications

- [Console CLI Specification](spec-app-console-cli-interface.md)
- [Semantic Model Storage](../docs/technical/SEMANTIC_MODEL_STORAGE.md)
- [Project Structure](spec-project-structure-genai-database-explorer.md)
- [Microsoft Semantic Kernel](https://docs.microsoft.com/en-us/semantic-kernel/)
- [Prompty Documentation](https://github.com/microsoft/prompty)
