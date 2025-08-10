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
    [Ignore("Pending proper GeneratedEmbeddings mock/fake – returns type differs across packages; follow-up to provide a concrete fake implementation.")]
    public async Task GenerateAsync_Should_Return_Embedding_From_Service()
    {
        // Arrange
        var factory = new Mock<ISemanticKernelFactory>();
        // Build a real Kernel with DI so GetRequiredService works
        var builder = Kernel.CreateBuilder();
        var services = builder.Services;

        // Register a placeholder embedding generator; test is ignored until a proper fake is provided
        var placeholder = new Mock<IEmbeddingGenerator<string, Embedding<float>>>().Object;
        services.AddSingleton(placeholder);
        var kernel = builder.Build();
        factory.Setup(f => f.CreateSemanticKernel()).Returns(kernel);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<SemanticKernelEmbeddingGenerator>>();
        var sut = new SemanticKernelEmbeddingGenerator(factory.Object, logger.Object);

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
        var sut = new SemanticKernelEmbeddingGenerator(factory.Object, logger.Object);
        var result = await sut.GenerateAsync("hello", new SemanticVectors.Infrastructure.VectorInfrastructure(
            Provider: "InMemory",
            CollectionName: "test-collection",
            EmbeddingServiceId: "Missing",
            Settings: new GenAIDBExplorer.Core.Models.Project.VectorIndexSettings()
        ));
        result.Length.Should().Be(0);
    }
}
