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
public class StoredProcedureEndpointsTests
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
    public async Task GetStoredProcedures_ShouldReturnPaginatedList()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/stored-procedures?offset=0&limit=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<EntitySummaryResponse>>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetStoredProcedureDetail_ShouldReturnFullDetails()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/stored-procedures/dbo/uspGetCustomers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StoredProcedureDetailResponse>();
        result.Should().NotBeNull();
        result!.Schema.Should().Be("dbo");
        result.Name.Should().Be("uspGetCustomers");
        result.Parameters.Should().Be("@Param1 INT");
        result.Definition.Should().Contain("SELECT");
    }

    [TestMethod]
    public async Task GetStoredProcedureDetail_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/stored-procedures/dbo/NonExistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetStoredProcedures_WhenModelNotLoaded_ShouldReturn503()
    {
        // Arrange
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(false);
        _factory.MockCacheService.Setup(c => c.GetModelAsync())
            .ThrowsAsync(new InvalidOperationException("Model not loaded"));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/stored-procedures");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task PatchStoredProcedure_ShouldUpdateDescription()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        _factory.MockRepository.Setup(r => r.SaveChangesAsync(
            It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>(), null))
            .Returns(Task.CompletedTask);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest("Updated sproc desc", null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/stored-procedures/dbo/uspGetCustomers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StoredProcedureDetailResponse>();
        result.Should().NotBeNull();
        result!.Description.Should().Be("Updated sproc desc");
    }

    [TestMethod]
    public async Task PatchStoredProcedure_ShouldUpdateSemanticDescription()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        _factory.MockRepository.Setup(r => r.SaveChangesAsync(
            It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>(), null))
            .Returns(Task.CompletedTask);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest(null, "Updated semantic desc");

        // Act
        var response = await client.PatchAsJsonAsync("/api/stored-procedures/dbo/uspGetCustomers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StoredProcedureDetailResponse>();
        result.Should().NotBeNull();
        result!.SemanticDescription.Should().Be("Updated semantic desc");
    }

    [TestMethod]
    public async Task PatchStoredProcedure_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest("Desc", null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/stored-procedures/dbo/NonExistent", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task PatchStoredProcedure_WhenBothFieldsNull_ShouldReturn400()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        var request = new UpdateEntityDescriptionRequest(null, null);

        // Act
        var response = await client.PatchAsJsonAsync("/api/stored-procedures/dbo/uspGetCustomers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
