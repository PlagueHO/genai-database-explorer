using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using GenAIDBExplorer.Core.SemanticKernel;
using Moq;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;

namespace GenAIDBExplorer.Core.Test.VectorEmbedding.Services;

[TestClass]
public class SemanticKernelEmbeddingGeneratorTests
{
    [TestMethod]
    public async Task GenerateAsync_Should_Return_Embedding_From_Service()
    {
        // Arrange
        var factory = new Mock<ISemanticKernelFactory>();
        // Build a real Kernel with DI so GetRequiredService works
        var builder = Kernel.CreateBuilder();
        var services = builder.Services;

    // Register a mock embedding generator keyed as "Embeddings" with a deterministic vector.
    // The production code resolves IEmbeddingGenerator via Microsoft.Extensions.AI DI, using a service key.
    // Returning GeneratedEmbeddings ensures the code path matches the real interface and keeps the test hermetic.
        var mockGen = new Moq.Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        mockGen
            .Setup(g => g.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<string> inputs, EmbeddingGenerationOptions? opts, CancellationToken ct) =>
            {
                var gen = new GeneratedEmbeddings<Embedding<float>>(
                    new List<Embedding<float>> { new Embedding<float>(new float[] { 0.1f, 0.2f, 0.3f }) });
                return Task.FromResult(gen);
            });
        services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>("Embeddings", mockGen.Object);
        var kernel = builder.Build();
        factory.Setup(f => f.CreateSemanticKernel()).Returns(kernel);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<SemanticKernelEmbeddingGenerator>>();
        var perf = new Mock<GenAIDBExplorer.Core.Repository.Performance.IPerformanceMonitor>();
        perf.Setup(p => p.StartOperation(It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>()))
            .Returns(new GenAIDBExplorer.Core.Repository.Performance.PerformanceTrackingContext("test", new GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor(new Mock<Microsoft.Extensions.Logging.ILogger<GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor>>().Object), new Dictionary<string, object>()));
        var sut = new SemanticKernelEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        var result = await sut.GenerateAsync("hello", new SemanticVectors.Infrastructure.VectorInfrastructure(
            Provider: "InMemory",
            CollectionName: "test-collection",
            EmbeddingServiceId: "Embeddings",
            Settings: new GenAIDBExplorer.Core.Models.Project.VectorIndexSettings()
        ));

        // Assert
        result.Length.Should().Be(3);
        result.ToArray().Should().BeEquivalentTo(new[] { 0.1f, 0.2f, 0.3f });
    }

    [TestMethod]
    public async Task GenerateAsync_Should_Return_Empty_On_Missing_Service()
    {
        var factory = new Mock<ISemanticKernelFactory>();
        // Build kernel without registering an embedding generator
        var builder = Kernel.CreateBuilder();
        var kernel = builder.Build();
        factory.Setup(f => f.CreateSemanticKernel()).Returns(kernel);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<SemanticKernelEmbeddingGenerator>>();
        var perf = new Mock<GenAIDBExplorer.Core.Repository.Performance.IPerformanceMonitor>();
        perf.Setup(p => p.StartOperation(It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>()))
            .Returns(new GenAIDBExplorer.Core.Repository.Performance.PerformanceTrackingContext("test", new GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor(new Mock<Microsoft.Extensions.Logging.ILogger<GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor>>().Object), new Dictionary<string, object>()));
        var sut = new SemanticKernelEmbeddingGenerator(factory.Object, logger.Object, perf.Object);
        var result = await sut.GenerateAsync("hello", new SemanticVectors.Infrastructure.VectorInfrastructure(
            Provider: "InMemory",
            CollectionName: "test-collection",
            EmbeddingServiceId: "Missing",
            Settings: new GenAIDBExplorer.Core.Models.Project.VectorIndexSettings()
        ));
        result.Length.Should().Be(0);
    }
}
