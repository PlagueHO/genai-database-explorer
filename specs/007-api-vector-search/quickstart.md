# Quickstart: API Vector Search Endpoint

**Feature**: 007-api-vector-search

## Prerequisites

1. The solution builds successfully: `dotnet build genai-database-explorer-service/GenAIDBExplorer.slnx`
2. A project folder exists with `settings.json` containing valid OpenAI/Azure AI Foundry configuration
3. The semantic model has been extracted (`extract-model`) and vector embeddings generated (`generate-vectors`)

## What Gets Built

### Core Layer (`GenAIDBExplorer.Core`)

1. **New method on `ISemanticModelSearchService`**: `SearchAsync(string query, int topK, IReadOnlyList<string>? entityTypes, CancellationToken)` — unified cross-type search with score threshold filtering
2. **New DI extension**: `AddGenAIDBExplorerVectorSearchServices()` in `ServiceRegistrationExtensions.cs` — registers search-only vector services (not generation services)

### API Layer (`GenAIDBExplorer.Api`)

3. **New endpoint class**: `SearchEndpoints.cs` in `Endpoints/` — maps `POST /api/search` with OpenAPI metadata
4. **New request model**: `SearchRequest.cs` in `Models/` — represents the search JSON body
5. **New response models**: `SearchResultResponse.cs` and `SearchResponse.cs` in `Models/` — search result DTOs
6. **DI registration**: `Program.cs` calls `AddGenAIDBExplorerVectorSearchServices()` and wires search endpoint

### Test Layer

7. **`SearchEndpointsTests.cs`** in `GenAIDBExplorer.Api.Test/Endpoints/` — HTTP-level tests for the endpoint (routing, validation, serialization)
8. **Updated `TestApiFactory.cs`** — adds `Mock<ISemanticModelSearchService>` for endpoint test isolation
9. **Updated `SemanticModelSearchServiceTests.cs`** in `GenAIDBExplorer.Core.Test/` — tests for the new unified `SearchAsync` method

## Implementation Order

The test-first workflow (per constitution) means tests are written before implementation for each step:

1. **Core: `ISemanticModelSearchService.SearchAsync()`** — Add unified method signature, then implementation with min-score threshold
2. **Core: `AddGenAIDBExplorerVectorSearchServices()`** — DI extension for search-only services
3. **API: Request/response models** — `SearchRequest`, `SearchResultResponse`, `SearchResponse`
4. **API: `SearchEndpoints`** — Endpoint class with validation and mapping
5. **API: `Program.cs` wiring** — Register search services and map search endpoint

## Key Patterns to Follow

### Endpoint Pattern (from `TableEndpoints.cs`)

```csharp
public static class SearchEndpoints
{
    public static WebApplication MapSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/search").WithTags("Search");

        group.MapPost("/", SearchEntities)
            .WithName("SearchEntities")
            .WithDescription("Search semantic model entities using natural language")
            .Produces<SearchResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }
}
```

### Test Pattern (from `TableEndpointsTests.cs`)

```csharp
[TestMethod]
public async Task SearchEntities_ValidQuery_ReturnsResults()
{
    // Arrange
    Factory.MockSearchService.Setup(s => s.SearchAsync(...))
        .ReturnsAsync(new List<SemanticModelSearchResult> { ... });

    // Act
    var response = await Client.PostAsJsonAsync("/api/search", new { query = "customers" });

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
    result!.Results.Should().HaveCount(1);
}
```

### DI Pattern (from `ServiceRegistrationExtensions.cs`)

```csharp
public static IServiceCollection AddGenAIDBExplorerVectorSearchServices(
    this IServiceCollection services)
{
    services.AddSingleton<IVectorIndexPolicy, VectorIndexPolicy>();
    services.AddSingleton<IVectorInfrastructureFactory, VectorInfrastructureFactory>();
    services.AddSingleton<IEmbeddingGenerator, ChatClientEmbeddingGenerator>();
    services.AddSingleton<IVectorSearchService, SkInMemoryVectorSearchService>();
    services.AddSingleton<ISemanticModelSearchService, SemanticModelSearchService>();
    return services;
}
```

## Verification

```bash
# Build
dotnet build genai-database-explorer-service/GenAIDBExplorer.slnx

# Run unit tests
dotnet exec genai-database-explorer-service/tests/unit/GenAIDBExplorer.Core.Test/bin/Debug/net10.0/GenAIDBExplorer.Core.Test.dll
dotnet exec genai-database-explorer-service/tests/unit/GenAIDBExplorer.Api.Test/bin/Debug/net10.0/GenAIDBExplorer.Api.Test.dll

# Manual test (with running API)
curl -X POST http://localhost:5000/api/search \
  -H "Content-Type: application/json" \
  -d '{"query": "customer orders", "limit": 5}'
```
