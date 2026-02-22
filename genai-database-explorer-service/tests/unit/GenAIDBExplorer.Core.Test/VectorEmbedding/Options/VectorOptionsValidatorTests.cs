using FluentAssertions;
using GenAIDBExplorer.Core.SemanticVectors.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.VectorEmbedding.Options;

[TestClass]
public class VectorOptionsValidatorTests
{
    [TestMethod]
    public void Validate_Should_Fail_On_Invalid_Provider()
    {
        var validator = new VectorOptionsValidator();
        var result = validator.Validate(null, new VectorIndexOptions { Provider = "Bad", CollectionName = "c", EmbeddingServiceId = "Embeddings" });
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Provider must be one of"));
    }

    [TestMethod]
    public void Validate_Should_Pass_On_Valid_InMemory()
    {
        var validator = new VectorOptionsValidator();
        var result = validator.Validate(null, new VectorIndexOptions { Provider = "InMemory", CollectionName = "c", EmbeddingServiceId = "Embeddings", ExpectedDimensions = 10 });
        result.Succeeded.Should().BeTrue();
    }

    [TestMethod]
    public void Validate_Should_Fail_When_CosmosDB_VectorPath_Missing()
    {
        var validator = new VectorOptionsValidator();
        var options = new VectorIndexOptions { Provider = "CosmosDB", CollectionName = "c", EmbeddingServiceId = "Embeddings" };
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("CosmosDB.VectorPath is required"));
    }

    [TestMethod]
    public void Validate_Should_Fail_When_CosmosDB_Distance_Function_Invalid()
    {
        var validator = new VectorOptionsValidator();
        var options = new VectorIndexOptions { Provider = "CosmosDB", CollectionName = "c", EmbeddingServiceId = "Embeddings", CosmosDB = new VectorIndexOptions.CosmosDBOptions { VectorPath = "/v", DistanceFunction = "chebyshev" } };
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("CosmosDB.DistanceFunction must be one of"));
    }

    [TestMethod]
    public void Validate_Should_Fail_When_CosmosDB_IndexType_Invalid()
    {
        var validator = new VectorOptionsValidator();
        var options = new VectorIndexOptions { Provider = "CosmosDB", CollectionName = "c", EmbeddingServiceId = "Embeddings", CosmosDB = new VectorIndexOptions.CosmosDBOptions { VectorPath = "/v", IndexType = "hnsw" } };
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("CosmosDB.IndexType must be one of"));
    }

    [TestMethod]
    public void Validate_Should_Pass_When_CosmosDB_Values_Valid()
    {
        var validator = new VectorOptionsValidator();
        var options = new VectorIndexOptions { Provider = "CosmosDB", CollectionName = "c", EmbeddingServiceId = "Embeddings", CosmosDB = new VectorIndexOptions.CosmosDBOptions { VectorPath = "/v", DistanceFunction = "cosine", IndexType = "diskANN" }, ExpectedDimensions = 1536 };
        var result = validator.Validate(null, options);
        result.Succeeded.Should().BeTrue();
    }
}
