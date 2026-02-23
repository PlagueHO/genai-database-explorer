using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Api.Test.Infrastructure;
using Moq;

namespace GenAIDBExplorer.Api.Test.Endpoints;

[TestClass]
public class ProjectEndpointsTests
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
    public async Task GetProject_ShouldReturnProjectInfo()
    {
        // Arrange
        var model = TestData.CreateTestModel();
        _factory.MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(model);
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(true);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/project");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProjectInfoResponse>();
        result.Should().NotBeNull();
        result!.ModelLoaded.Should().BeTrue();
        result.ProjectPath.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task GetProject_WhenModelNotLoaded_ShouldStillReturn200()
    {
        // Arrange
        _factory.MockCacheService.Setup(c => c.IsLoaded).Returns(false);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/project");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProjectInfoResponse>();
        result.Should().NotBeNull();
        result!.ModelLoaded.Should().BeFalse();
    }
}
