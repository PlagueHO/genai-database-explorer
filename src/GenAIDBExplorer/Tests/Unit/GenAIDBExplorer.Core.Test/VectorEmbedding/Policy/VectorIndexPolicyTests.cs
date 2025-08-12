using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenAIDBExplorer.Core.SemanticVectors.Policy;
using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Core.Test.VectorEmbedding.Policy;

[TestClass]
public class VectorIndexPolicyTests
{
    [TestMethod]
    public void ResolveProvider_Auto_Should_Select_InMemory_When_Not_Cosmos()
    {
        // Arrange
        var policy = new VectorIndexPolicy();
        var settings = new VectorIndexSettings
        {
            Provider = "Auto",
            CollectionName = "genaide-entities",
            EmbeddingServiceId = "Embeddings",
            ExpectedDimensions = 3072
        };

        // Act
        var provider = policy.ResolveProvider(settings, repositoryStrategy: "LocalDisk");

        // Assert
        provider.Should().Be("InMemory");
    }

    [TestMethod]
    public void ResolveProvider_Auto_Should_Select_Cosmos_When_Repo_Cosmos()
    {
        var policy = new VectorIndexPolicy();
        var settings = new VectorIndexSettings { Provider = "Auto" };
        var provider = policy.ResolveProvider(settings, repositoryStrategy: "Cosmos");
    provider.Should().Be("CosmosDB");
    }

    [TestMethod]
    public void Validate_Should_Enforce_Cosmos_Constraint()
    {
        var policy = new VectorIndexPolicy();
        var settings = new VectorIndexSettings { Provider = "InMemory" };
        Action act = () => policy.Validate(settings, repositoryStrategy: "Cosmos");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cosmos persistence requires CosmosDB*");
    }
}
