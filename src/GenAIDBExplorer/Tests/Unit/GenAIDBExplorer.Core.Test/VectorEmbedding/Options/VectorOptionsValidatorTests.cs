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
}
