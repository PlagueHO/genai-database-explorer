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
public class ViewEndpointsTests
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
    public async Task GetViews_ShouldReturnPaginatedList()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/views?offset=0&limit=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<EntitySummaryResponse>>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetViewDetail_ShouldReturnFullDetails()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/views/SalesLT/vProductAndDescription");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ViewDetailResponse>();
        result.Should().NotBeNull();
        result!.Schema.Should().Be("SalesLT");
        result.Name.Should().Be("vProductAndDescription");
    }

    [TestMethod]
    public async Task GetViewDetail_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/views/SalesLT/NonExistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetViews_WhenModelNotLoaded_ShouldReturn503()
    {
        // Arrange
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(false);
        _factory.MockCacheService.Setup(c => c.GetModelAsync())
            .ThrowsAsync(new InvalidOperationException("Model not loaded"));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/views");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task PatchView_ShouldUpdateDescription()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        _factory.MockRepository.Setup(r => r.SaveChangesAsync(
            It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>(), null))
            .Returns(Task.CompletedTask);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest("Updated view desc", null, null, null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/views/SalesLT/vProductAndDescription", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ViewDetailResponse>();
        result.Should().NotBeNull();
        result!.Description.Should().Be("Updated view desc");
    }

    [TestMethod]
    public async Task PatchView_ShouldUpdateSemanticDescription()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        _factory.MockRepository.Setup(r => r.SaveChangesAsync(
            It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>(), null))
            .Returns(Task.CompletedTask);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest(null, "Updated semantic desc", null, null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/views/SalesLT/vProductAndDescription", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ViewDetailResponse>();
        result.Should().NotBeNull();
        result!.SemanticDescription.Should().Be("Updated semantic desc");
    }

    [TestMethod]
    public async Task PatchView_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest("Desc", null, null, null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/views/SalesLT/NonExistent", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task PatchView_WhenBothFieldsNull_ShouldReturn400()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest(null, null, null, null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/views/SalesLT/vProductAndDescription", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
