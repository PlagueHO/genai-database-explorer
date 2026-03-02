using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Api.Test.Infrastructure;
using GenAIDBExplorer.Core.SemanticModelQuery;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GenAIDBExplorer.Api.Test.Endpoints;

[TestClass]
public class SearchEndpointsTests
{
    private static TestApiFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _factory = new TestApiFactory();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory?.Dispose();
    }

    [TestInitialize]
    public void TestInit()
    {
        _factory.ResetMocks();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Phase 2 (US1) — Basic search behaviour
    // ──────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SearchEntities_ValidQuery_ReturnsOkWithResults()
    {
        // Arrange
        var searchResults = new List<SemanticModelSearchResult>
        {
            new("Table", "SalesLT", "Customer", "Customer information", 0.87),
            new("View", "SalesLT", "vGetAllCategories", "Product categories", 0.72),
        };
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "customer" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
        result.Should().NotBeNull();
        result!.TotalResults.Should().Be(2);
        result.Results.Should().HaveCount(2);
        result.Results[0].EntityType.Should().Be("Table");
        result.Results[0].Schema.Should().Be("SalesLT");
        result.Results[0].Name.Should().Be("Customer");
        result.Results[0].Score.Should().Be(0.87);
    }

    [TestMethod]
    public async Task SearchEntities_NoMatches_ReturnsEmptyResults()
    {
        // Arrange
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SemanticModelSearchResult>());
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "xyz123abc" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
        result.Should().NotBeNull();
        result!.TotalResults.Should().Be(0);
        result.Results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SearchEntities_ServiceThrowsException_Returns503()
    {
        // Arrange
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Vector store unavailable"));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "customer" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Phase 3 (US2) — Limit parameter
    // ──────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SearchEntities_LimitOf5_CallsServiceWithTopK5()
    {
        // Arrange
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SemanticModelSearchResult>());
        using var client = _factory.CreateClient();

        // Act
        await client.PostAsJsonAsync("/api/search", new { query = "products", limit = 5 });

        // Assert
        _factory.MockSearchService.Verify(
            s => s.SearchAsync(It.IsAny<string>(), 5, It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchEntities_LimitAbove10_ClampedTo10()
    {
        // Arrange
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SemanticModelSearchResult>());
        using var client = _factory.CreateClient();

        // Act
        await client.PostAsJsonAsync("/api/search", new { query = "products", limit = 15 });

        // Assert — limit should be clamped to 10
        _factory.MockSearchService.Verify(
            s => s.SearchAsync(It.IsAny<string>(), 10, It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchEntities_NoLimit_DefaultsTo10()
    {
        // Arrange
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SemanticModelSearchResult>());
        using var client = _factory.CreateClient();

        // Act
        await client.PostAsJsonAsync("/api/search", new { query = "products" });

        // Assert
        _factory.MockSearchService.Verify(
            s => s.SearchAsync(It.IsAny<string>(), 10, It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchEntities_LimitZero_Returns400()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "products", limit = 0 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SearchEntities_NegativeLimit_Returns400()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "products", limit = -1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Phase 4 (US3) — Entity type filtering
    // ──────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SearchEntities_FilterByTable_PassesFilterToService()
    {
        // Arrange
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SemanticModelSearchResult>());
        using var client = _factory.CreateClient();

        // Act
        await client.PostAsJsonAsync("/api/search", new { query = "customers", entityTypes = new[] { "table" } });

        // Assert
        _factory.MockSearchService.Verify(
            s => s.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.Is<IReadOnlyList<string>?>(et => et != null && et.Contains("table")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchEntities_NoFilter_PassesNullToService()
    {
        // Arrange
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SemanticModelSearchResult>());
        using var client = _factory.CreateClient();

        // Act
        await client.PostAsJsonAsync("/api/search", new { query = "customers" });

        // Assert — no entityTypes property → null passed to service
        _factory.MockSearchService.Verify(
            s => s.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.Is<IReadOnlyList<string>?>(et => et == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchEntities_InvalidEntityType_Returns400()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "customers", entityTypes = new[] { "invalid" } });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Contain("table");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Phase 5 (US4) — Graceful error handling
    // ──────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SearchEntities_EmptyVectorStore_ReturnsEmptyResults()
    {
        // Arrange
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SemanticModelSearchResult>());
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "anything" });

        // Assert — empty store is not an error
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
        result!.Results.Should().BeEmpty();
        result.TotalResults.Should().Be(0);
    }

    [TestMethod]
    public async Task SearchEntities_InfrastructureFailure_Returns503WithProblemDetails()
    {
        // Arrange
        _factory.MockSearchService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Infrastructure failure"));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "customer" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Service Unavailable");
        problem.Detail.Should().Be("The semantic model search service is not currently available.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Phase 6 — Edge cases and input validation
    // ──────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SearchEntities_EmptyQuery_Returns400()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SearchEntities_WhitespaceQuery_Returns400()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = "   " });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SearchEntities_NullQuery_Returns400()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act — omit "query" field entirely
        var response = await client.PostAsJsonAsync("/api/search", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SearchEntities_ExcessivelyLongQuery_Returns400()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var longQuery = new string('a', 2001);

        // Act
        var response = await client.PostAsJsonAsync("/api/search", new { query = longQuery });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
