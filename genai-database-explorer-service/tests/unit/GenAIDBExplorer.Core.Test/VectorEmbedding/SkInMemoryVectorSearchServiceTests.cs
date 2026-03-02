using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Repository.Performance;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Search;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.VectorEmbedding;

[TestClass]
public class SkInMemoryVectorSearchServiceTests
{
    private SkInMemoryVectorSearchService CreateService()
    {
        var store = new InMemoryVectorStore();
        var perfMonitor = new PerformanceMonitor(
            new Mock<ILogger<PerformanceMonitor>>().Object);
        return new SkInMemoryVectorSearchService(store, perfMonitor);
    }

    [TestMethod]
    public async Task SearchAsync_NonInMemoryProvider_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var vector = new ReadOnlyMemory<float>([1.0f, 2.0f, 3.0f]);
        var infrastructure = new VectorInfrastructure(
            Provider: "AzureAISearch",
            CollectionName: "test-collection",
            EmbeddingServiceId: "Embeddings",
            Settings: new VectorIndexSettings());

        // Act
        var act = () => service.SearchAsync(vector, topK: 5, infrastructure);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*InMemory*AzureAISearch*");
    }

    [TestMethod]
    public async Task SearchAsync_InMemoryProvider_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();
        var vector = new ReadOnlyMemory<float>([1.0f, 2.0f, 3.0f]);
        var infrastructure = new VectorInfrastructure(
            Provider: "InMemory",
            CollectionName: "test-collection",
            EmbeddingServiceId: "Embeddings",
            Settings: new VectorIndexSettings());

        // Act
        var results = await service.SearchAsync(vector, topK: 5, infrastructure);

        // Assert
        results.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SearchAsync_CosmosDBProvider_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var vector = new ReadOnlyMemory<float>([1.0f, 2.0f, 3.0f]);
        var infrastructure = new VectorInfrastructure(
            Provider: "CosmosDB",
            CollectionName: "test-collection",
            EmbeddingServiceId: "Embeddings",
            Settings: new VectorIndexSettings());

        // Act
        var act = () => service.SearchAsync(vector, topK: 5, infrastructure);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*InMemory*CosmosDB*");
    }
}
