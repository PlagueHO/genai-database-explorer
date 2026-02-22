using System.Text.Json;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.DTO;
using GenAIDBExplorer.Core.Repository.Mappers;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.Repository;

[TestClass]
public class RepositoryDtoShapeTests
{
    [TestMethod]
    public async Task LocalBlob_Envelope_ShouldContain_Vector_And_Metadata()
    {
        var table = new SemanticModelTable("dbo", "X");
        var payload = new EmbeddingPayload
        {
            Vector = new float[] { 0.1f, 0.2f },
            Metadata = new EmbeddingMetadata { ContentHash = "abc", ModelId = "Embeddings" }
        };
        var mapper = new LocalBlobEntityMapper();
        var env = mapper.ToPersistedEntity(table, payload);
        var serializer = new SecureJsonSerializer(new Moq.Mock<Microsoft.Extensions.Logging.ILogger<SecureJsonSerializer>>().Object);
        var json = await serializer.SerializeAsync(env, new JsonSerializerOptions { WriteIndented = false });

        json.Should().Contain("\"embedding\"");
        json.Should().Contain("\"vector\"");
        json.Should().Contain("\"metadata\"");
        json.Should().Contain("\"contentHash\"");
        json.Should().Contain("abc");
    }

    [TestMethod]
    public void Cosmos_Document_ShouldNotContain_Vectors()
    {
        var table = new SemanticModelTable("dbo", "Y");
        var mapper = new CosmosDbEntityMapper();
        var dto = mapper.ToCosmosDbEntity("M", "table", "dbo.Y", table, new EmbeddingMetadata { ContentHash = "xyz" });

        var json = System.Text.Json.JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = false });

        json.Should().Contain("\"embedding\"");
        json.Should().Contain("\"contentHash\"");
        json.Should().NotContain("\"vector\"");
    }
}
