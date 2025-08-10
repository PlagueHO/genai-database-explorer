using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenAIDBExplorer.Core.SemanticVectors.Mapping;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Test.VectorEmbedding.Mapping;

[TestClass]
public class VectorRecordMapperTests
{
    [TestMethod]
    public void BuildEntityText_Should_Include_Columns_And_Description()
    {
        // Arrange
        var mapper = new VectorRecordMapper();
        var table = new SemanticModelTable("dbo", "Customer", description: "Contains customer data");
        table.Columns.Add(new SemanticModelColumn("dbo", "Id") { Type = "int", Description = "Primary key", IsPrimaryKey = true });
        table.Columns.Add(new SemanticModelColumn("dbo", "Name") { Type = "nvarchar(100)", Description = "Customer name" });

        // Act
        var text = mapper.BuildEntityText(table);

        // Assert
        text.Should().Contain("Schema: dbo");
        text.Should().Contain("Name: Customer");
        text.Should().Contain("Description:");
        text.Should().Contain("Columns:");
        // Expect column lines as "- {Name}: {Type} - {Description}"
        text.Should().Contain("- Id: int - Primary key");
        text.Should().Contain("- Name: nvarchar(100) - Customer name");
    }

    [TestMethod]
    public void ToRecord_Should_Map_Metadata_And_Vector()
    {
        var mapper = new VectorRecordMapper();
        var table = new SemanticModelTable("dbo", "Customer");
        var content = "some content";
        var vector = new ReadOnlyMemory<float>(Enumerable.Repeat(0.1f, 4).ToArray());
        var record = mapper.ToRecord(table, id: "m:e:s:n", content, vector, contentHash: "abc123");

        record.Should().NotBeNull();
        record.Id.Should().Be("m:e:s:n");
        record.Content.Should().Be(content);
        record.Vector.Length.Should().Be(4);
        record.Schema.Should().Be("dbo");
        record.Name.Should().Be("Customer");
        record.EntityType.Should().Be("SemanticModelTable");
        record.ContentHash.Should().Be("abc123");
    }
}
