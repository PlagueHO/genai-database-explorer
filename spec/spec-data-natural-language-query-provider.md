---
title: Natural Language Query Provider Specification
version: 2.1
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

- **GUD-001**: Use Semantic Kernel for AI orchestration and Prompty files for templates
- **GUD-002**: Implement async/await, dependency injection, structured logging
- **GUD-003**: Apply Provider, Strategy, Chain of Responsibility patterns

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
```

### Key Data Contracts

```csharp
public class QuestionResult
{
    public string OriginalQuestion { get; set; }
    public QuestionType QuestionType { get; set; }
    public QuestionComplexity Complexity { get; set; }
    public string Answer { get; set; }
    public List<string> ReasoningSteps { get; set; } = new();
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public ConversationContext? UpdatedContext { get; set; }
}

public class ConversationContext
{
    public string ConversationId { get; set; }
    public List<string> History { get; set; } = new();
    public Dictionary<string, object> SharedContext { get; set; } = new();
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
2. "Show me all orders from last month" - Basic SQL generation
3. "What data type is CustomerID?" - Column metadata
4. "How many records in Products table?" - Count aggregation

### Moderate (2-3 Steps)

1. "What does GetCustomerOrderHistory stored procedure do?" - Business logic
2. "How are customers related to orders?" - Relationship analysis
3. "Which tables have most foreign keys?" - Schema patterns
4. "Better way to write: SELECT * FROM Orders WHERE YEAR(OrderDate) = 2024?" - Basic optimization

### Complex (Multi-Step Analysis)

1. "Analyze data model for performance bottlenecks" - Schema analysis
2. "What happens if I delete a customer record?" - Impact analysis
3. "Compare approaches to find customers with no orders" - Query alternatives
4. "How does order fulfillment work via procedures/triggers?" - Process analysis

### Advanced (Deep Reasoning)

1. "Design strategy to identify churning customers by order patterns" - Predictive design
2. "What data quality issues exist and how to fix?" - Quality assessment
3. "Redesign product tables for multi-language support" - Architecture
4. "Analyze indexing strategy for common query patterns" - Performance review

### Educational/Comparative

1. "Explain clustered vs non-clustered indexes with examples" - Education
2. "Why choose stored procedure vs view for this use case?" - Design rationale

## 6. Usage Examples

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

## 7. Implementation Notes

### Technology Choices
- **Semantic Kernel**: AI orchestration, multi-step reasoning, conversation management
- **ReAct Pattern**: Break complex questions into Think→Learn→Act cycles
- **Prompty Files**: Version-controlled AI prompts for different question types
- **Conversation Context**: Handle follow-up questions and maintain analysis state

### Key Patterns
- **Question Classification**: Route to appropriate processing engine based on complexity
- **Multi-Modal Analysis**: Combine schema, business logic, and data insights
- **Graceful Degradation**: Provide partial results when complete analysis fails
- **Safety First**: Validate all SQL for injection attacks, allow only safe operations

## 8. Dependencies

- **.NET 9**: Target runtime
- **Microsoft.SemanticKernel**: AI orchestration
- **Azure OpenAI**: Primary AI service
- **System.Data.Common**: Database connectivity
- **Semantic Model**: Enhanced database schema with business context

## 9. Validation Criteria

### Functional
- All question types processed correctly (SQL, schema, business logic, optimization)
- Multi-step reasoning follows logical progression
- Conversation context maintained across interactions
- Security validation blocks unsafe queries

### Performance
- Simple questions ≤15s, complex ≤60s
- Memory stable during reasoning chains
- Concurrent requests handled efficiently

### Quality
- Generated SQL produces expected results
- Explanations are clear and accurate
- ReAct reasoning provides logical steps
- Error messages offer actionable guidance

## 10. Related Specifications

- [Console CLI Specification](spec-app-console-cli-interface.md)
- [Semantic Model Storage](../docs/technical/SEMANTIC_MODEL_STORAGE.md)
- [Project Structure](spec-project-structure-genai-database-explorer.md)
- [Microsoft Semantic Kernel](https://docs.microsoft.com/en-us/semantic-kernel/)
- [Prompty Documentation](https://github.com/microsoft/prompty)
