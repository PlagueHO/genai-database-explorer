using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Api.Test.Infrastructure;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Moq;

namespace GenAIDBExplorer.Api.Test.Endpoints;

[TestClass]
public class ModelEndpointsTests
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
    public async Task GetModel_WhenModelLoaded_ShouldReturnSummary()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/model");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<SemanticModelSummaryResponse>();
        summary.Should().NotBeNull();
        summary!.Name.Should().Be("TestModel");
        summary.Source.Should().Be("TestSource");
        summary.TableCount.Should().Be(2);
        summary.ViewCount.Should().Be(1);
        summary.StoredProcedureCount.Should().Be(1);
    }

    [TestMethod]
    public async Task GetModel_WhenModelNotLoaded_ShouldReturn503()
    {
        // Arrange
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(false);
        _factory.MockCacheService.Setup(c => c.GetModelAsync())
            .ThrowsAsync(new InvalidOperationException("Model not loaded"));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/model");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task PostReload_ShouldReloadAndReturnSummary()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.ReloadModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/model/reload", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<SemanticModelSummaryResponse>();
        summary.Should().NotBeNull();
        summary!.Name.Should().Be("TestModel");
        _factory.MockCacheService.Verify(c => c.ReloadModelAsync(), Times.Once);
    }
}
