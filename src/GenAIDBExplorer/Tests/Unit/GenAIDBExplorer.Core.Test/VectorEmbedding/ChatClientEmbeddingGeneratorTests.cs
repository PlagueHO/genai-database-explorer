using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Repository.Performance;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.VectorEmbedding;

[TestClass]
public class ChatClientEmbeddingGeneratorTests
{
    private static VectorInfrastructure CreateInfrastructure(string serviceId = "Embeddings") =>
        new(
            Provider: "InMemory",
            CollectionName: "test-collection",
            EmbeddingServiceId: serviceId,
            Settings: new VectorIndexSettings()
        );

    private static Mock<IPerformanceMonitor> CreatePerformanceMonitor()
    {
        var perf = new Mock<IPerformanceMonitor>();
        perf.Setup(p => p.StartOperation(It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>()))
            .Returns(new PerformanceTrackingContext(
                "test",
                new PerformanceMonitor(new Mock<ILogger<PerformanceMonitor>>().Object),
                new Dictionary<string, object>()));
        return perf;
    }

    private static Mock<IEmbeddingGenerator<string, Embedding<float>>> CreateMockEmbeddingGenerator(float[] vector)
    {
        var mockGen = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        mockGen
            .Setup(g => g.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedEmbeddings<Embedding<float>>(
                [new Embedding<float>(vector)]));
        return mockGen;
    }

    [TestMethod]
    public async Task GenerateAsync_ReturnsEmbeddingVector_WhenGeneratorSucceeds()
    {
        // Arrange
        var expectedVector = new float[] { 0.1f, 0.2f, 0.3f };
        var mockGen = CreateMockEmbeddingGenerator(expectedVector);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        var result = await sut.GenerateAsync("hello world", CreateInfrastructure());

        // Assert
        result.Length.Should().Be(3);
        result.ToArray().Should().BeEquivalentTo(expectedVector);
    }

    [TestMethod]
    public async Task GenerateAsync_CallsCreateEmbeddingGenerator()
    {
        // Arrange
        var mockGen = CreateMockEmbeddingGenerator([0.5f]);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        await sut.GenerateAsync("test", CreateInfrastructure());

        // Assert
        factory.Verify(f => f.CreateEmbeddingGenerator(), Times.Once);
    }

    [TestMethod]
    public async Task GenerateAsync_PassesTextToEmbeddingGenerator()
    {
        // Arrange
        var mockGen = CreateMockEmbeddingGenerator([0.5f]);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        await sut.GenerateAsync("specific input text", CreateInfrastructure());

        // Assert
        mockGen.Verify(g => g.GenerateAsync(
            It.Is<IEnumerable<string>>(texts => texts.First() == "specific input text"),
            It.IsAny<EmbeddingGenerationOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GenerateAsync_ThrowsArgumentException_WhenTextIsNull()
    {
        // Arrange
        var mockGen = CreateMockEmbeddingGenerator([0.5f]);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        var act = async () => await sut.GenerateAsync(null!, CreateInfrastructure());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("text");
    }

    [TestMethod]
    public async Task GenerateAsync_ThrowsArgumentException_WhenTextIsEmpty()
    {
        // Arrange
        var mockGen = CreateMockEmbeddingGenerator([0.5f]);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        var act = async () => await sut.GenerateAsync("", CreateInfrastructure());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("text");
    }

    [TestMethod]
    public async Task GenerateAsync_ThrowsArgumentException_WhenTextIsWhitespace()
    {
        // Arrange
        var mockGen = CreateMockEmbeddingGenerator([0.5f]);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        var act = async () => await sut.GenerateAsync("   ", CreateInfrastructure());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("text");
    }

    [TestMethod]
    public async Task GenerateAsync_ThrowsArgumentNullException_WhenInfrastructureIsNull()
    {
        // Arrange
        var mockGen = CreateMockEmbeddingGenerator([0.5f]);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        var act = async () => await sut.GenerateAsync("hello", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("infrastructure");
    }

    [TestMethod]
    public async Task GenerateAsync_ReturnsEmpty_WhenGeneratorReturnsNoResults()
    {
        // Arrange
        var mockGen = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        mockGen
            .Setup(g => g.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedEmbeddings<Embedding<float>>([]));

        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        var result = await sut.GenerateAsync("hello", CreateInfrastructure());

        // Assert
        result.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task GenerateAsync_StartsPerformanceOperation()
    {
        // Arrange
        var mockGen = CreateMockEmbeddingGenerator([0.5f]);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        await sut.GenerateAsync("hello", CreateInfrastructure());

        // Assert
        perf.Verify(p => p.StartOperation(
            "Vector.Embeddings.Generate",
            It.IsAny<IDictionary<string, object>?>()), Times.Once);
    }

    [TestMethod]
    public async Task GenerateAsync_PassesCancellationToken()
    {
        // Arrange
        var mockGen = CreateMockEmbeddingGenerator([0.5f]);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        using var cts = new CancellationTokenSource();
        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        await sut.GenerateAsync("hello", CreateInfrastructure(), cts.Token);

        // Assert
        mockGen.Verify(g => g.GenerateAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<EmbeddingGenerationOptions?>(),
            cts.Token), Times.Once);
    }

    [TestMethod]
    public async Task GenerateAsync_HighDimensionalVector_ReturnsCorrectLength()
    {
        // Arrange
        var expectedVector = Enumerable.Range(0, 1536).Select(i => (float)i / 1536f).ToArray();
        var mockGen = CreateMockEmbeddingGenerator(expectedVector);
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(f => f.CreateEmbeddingGenerator()).Returns(mockGen.Object);
        var logger = new Mock<ILogger<ChatClientEmbeddingGenerator>>();
        var perf = CreatePerformanceMonitor();

        var sut = new ChatClientEmbeddingGenerator(factory.Object, logger.Object, perf.Object);

        // Act
        var result = await sut.GenerateAsync("large embedding text", CreateInfrastructure());

        // Assert
        result.Length.Should().Be(1536);
    }
}
