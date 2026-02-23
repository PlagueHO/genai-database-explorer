using FluentAssertions;
using GenAIDBExplorer.Api.Health;
using GenAIDBExplorer.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace GenAIDBExplorer.Api.Test.Health;

[TestClass]
public class SemanticModelHealthCheckTests
{
    [TestMethod]
    public async Task CheckHealthAsync_WhenModelIsLoaded_ShouldReturnHealthy()
    {
        // Arrange
        var mockCacheService = new Mock<ISemanticModelCacheService>();
        mockCacheService.Setup(c => c.IsLoaded).Returns(true);
        var healthCheck = new SemanticModelHealthCheck(mockCacheService.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("loaded");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenModelIsNotLoaded_ShouldReturnUnhealthy()
    {
        // Arrange
        var mockCacheService = new Mock<ISemanticModelCacheService>();
        mockCacheService.Setup(c => c.IsLoaded).Returns(false);
        var healthCheck = new SemanticModelHealthCheck(mockCacheService.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not loaded");
    }
}
