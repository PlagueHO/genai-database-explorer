using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenAIDBExplorer.Core.SemanticVectors.Keys;

namespace GenAIDBExplorer.Core.Test.VectorEmbedding.Keys;

[TestClass]
public class EntityKeyBuilderTests
{
    [TestMethod]
    public void BuildKey_Should_NormalizeAndConcatenate()
    {
        // Arrange
        var builder = new EntityKeyBuilder();

        // Act (params: modelName, entityType, schema, name)
        var result = builder.BuildKey("adventureworkslt", "table", "dbo", "Customer-01");

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("adventureworkslt");
        result.Should().Contain("table");
        result.Should().Contain("dbo");
        result.Should().Contain("customer-01");
        result.Should().Be("adventureworkslt:table:dbo:customer-01");
    }

    [TestMethod]
    public void BuildKey_Should_Handle_Nulls_And_Whitespace()
    {
        // Arrange
        var builder = new EntityKeyBuilder();

        // Act
        var act = () => builder.BuildKey(null!, "  ", "Table", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void BuildContentHash_Should_Be_Deterministic()
    {
        var builder = new EntityKeyBuilder();
        var h1 = builder.BuildContentHash("hello world");
        var h2 = builder.BuildContentHash("hello world");
        h1.Should().Be(h2);
        h1.Should().MatchRegex("^[0-9a-f]{64}$");
    }
}
