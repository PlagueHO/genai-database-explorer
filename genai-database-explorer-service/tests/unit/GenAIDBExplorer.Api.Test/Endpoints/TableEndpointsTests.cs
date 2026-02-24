using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Api.Test.Infrastructure;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using Moq;

namespace GenAIDBExplorer.Api.Test.Endpoints;

[TestClass]
public class TableEndpointsTests
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

    [TestMethod]
    public async Task GetTables_ShouldReturnPaginatedList()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tables?offset=0&limit=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<EntitySummaryResponse>>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Offset.Should().Be(0);
        result.Limit.Should().Be(50);
    }

    [TestMethod]
    public async Task GetTables_WithPagination_ShouldRespectOffsetAndLimit()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tables?offset=1&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<EntitySummaryResponse>>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(1);
        result.Offset.Should().Be(1);
        result.Limit.Should().Be(1);
    }

    [TestMethod]
    public async Task GetTables_WithDefaultPagination_ShouldUseDefaults()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tables");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<EntitySummaryResponse>>();
        result.Should().NotBeNull();
        result!.Offset.Should().Be(0);
        result.Limit.Should().Be(50);
    }

    [TestMethod]
    public async Task GetTableDetail_ShouldReturnFullDetails()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tables/SalesLT/Product");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TableDetailResponse>();
        result.Should().NotBeNull();
        result!.Schema.Should().Be("SalesLT");
        result.Name.Should().Be("Product");
        result.Columns.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetTableDetail_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tables/SalesLT/NonExistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetTables_WhenModelNotLoaded_ShouldReturn503()
    {
        // Arrange
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(false);
        _factory.MockCacheService.Setup(c => c.GetModelAsync())
            .ThrowsAsync(new InvalidOperationException("Model not loaded"));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tables");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task PatchTable_ShouldUpdateDescription()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        _factory.MockRepository.Setup(r => r.SaveChangesAsync(
            It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>(), null))
            .Returns(Task.CompletedTask);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest("New description", null, null, null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/tables/SalesLT/Product", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TableDetailResponse>();
        result.Should().NotBeNull();
        result!.Description.Should().Be("New description");
    }

    [TestMethod]
    public async Task PatchTable_ShouldUpdateSemanticDescription()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        _factory.MockRepository.Setup(r => r.SaveChangesAsync(
            It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>(), null))
            .Returns(Task.CompletedTask);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest(null, "New semantic description", null, null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/tables/SalesLT/Product", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TableDetailResponse>();
        result.Should().NotBeNull();
        result!.SemanticDescription.Should().Be("New semantic description");
    }

    [TestMethod]
    public async Task PatchTable_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest("Desc", null, null, null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/tables/SalesLT/NonExistent", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task PatchTable_WhenBothFieldsNull_ShouldReturn400()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest(null, null, null, null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/tables/SalesLT/Product", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
