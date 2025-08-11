using System.Text.Json;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.Security;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using GenAIDBExplorer.Core.SemanticVectors.Indexing;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Keys;
using GenAIDBExplorer.Core.SemanticVectors.Mapping;
using GenAIDBExplorer.Core.SemanticVectors.Orchestration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.VectorEmbedding.Orchestration;

[TestClass]
public class VectorGenerationServiceTests
{
    [TestMethod]
    public async Task GenerateAsync_Should_Skip_When_ContentHash_Unchanged()
    {
        var tmp = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"vg-{Guid.NewGuid():N}"));
        tmp.Create();
        try
        {
            var proj = new ProjectSettings
            {
                SettingsVersion = new Version(1, 0),
                Database = new DatabaseSettings(),
                DataDictionary = new DataDictionarySettings(),
                OpenAIService = new OpenAIServiceSettings(),
                SemanticModel = new SemanticModelSettings { PersistenceStrategy = "LocalDisk" },
                SemanticModelRepository = new SemanticModelRepositorySettings
                {
                    LocalDisk = new LocalDiskConfiguration { Directory = "semantic-model" }
                },
                VectorIndex = new VectorIndexSettings { Provider = "InMemory", CollectionName = "test", EmbeddingServiceId = "Embeddings" }
            };
            var infraFactory = new Mock<IVectorInfrastructureFactory>();
            infraFactory
                .Setup(f => f.Create(It.IsAny<VectorIndexSettings>(), It.IsAny<string>()))
                .Returns(new VectorInfrastructure("InMemory", "test", "Embeddings", proj.VectorIndex));
            var mapper = new Mock<IVectorRecordMapper>();
            mapper.Setup(m => m.BuildEntityText(It.IsAny<SemanticModelEntity>())).Returns("hello world");
            mapper
                .Setup(m => m.ToRecord(
                    It.IsAny<SemanticModelEntity>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ReadOnlyMemory<float>>(),
                    It.IsAny<string>()))
                .Returns(new GenAIDBExplorer.Core.SemanticVectors.Records.EntityVectorRecord { Id = "id", Content = "hello" });
            var embed = new Mock<IEmbeddingGenerator>();
            embed
                .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), default))
                .Returns(Task.FromResult((ReadOnlyMemory<float>)new ReadOnlyMemory<float>(new float[] { 1, 2, 3 })));
            var key = new Mock<IEntityKeyBuilder>();
            key.Setup(k => k.BuildContentHash(It.IsAny<string>())).Returns("hash");
            key.Setup(k => k.BuildKey(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns("id");
            var writer = new Mock<IVectorIndexWriter>();
            var serializer = new SecureJsonSerializer(new Moq.Mock<Microsoft.Extensions.Logging.ILogger<SecureJsonSerializer>>().Object);
            var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<VectorGenerationService>>();
            var perf = new GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor(
                new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor>>()
                    .Object);
            var sut = new VectorGenerationService(
                proj,
                infraFactory.Object,
                mapper.Object,
                embed.Object,
                key.Object,
                writer.Object,
                serializer,
                logger.Object,
                perf
            );

            var model = new SemanticModel("M", "S");
            var table = new SemanticModelTable("dbo", "T");
            model.AddTable(table);

            // Match the default LocalDisk repository directory used by VectorGenerationService ("semantic-model")
            var modelDir = new DirectoryInfo(Path.Combine(tmp.FullName, "semantic-model"));
            modelDir.Create();
            var tableDir = new DirectoryInfo(Path.Combine(modelDir.FullName, "tables"));
            tableDir.Create();
            var file = new FileInfo(Path.Combine(tableDir.FullName, "dbo.T.json"));
            // Write a persisted envelope using the production mapper/serializer so shape matches exactly
            var persistedMapper = new GenAIDBExplorer.Core.Repository.Mappers.LocalBlobEntityMapper();
            var payload = new GenAIDBExplorer.Core.Repository.DTO.EmbeddingPayload
            {
                // Vector can be omitted; metadata is what the service checks
                Metadata = new GenAIDBExplorer.Core.Repository.DTO.EmbeddingMetadata
                {
                    ContentHash = "hash",
                    ModelId = "Embeddings",
                    Dimensions = 0,
                    GeneratedAt = DateTimeOffset.UtcNow,
                    ServiceId = "Embeddings",
                    Version = "1"
                }
            };
            var envelope = persistedMapper.ToPersistedEntity(table, payload);
            var json = await serializer.SerializeAsync(
                envelope,
                new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(file.FullName, json);

            // Verify the path the service will read matches the file we created
            var expectedPath = Path.Combine(tmp.FullName, proj.SemanticModelRepository!.LocalDisk!.Directory, "tables", "dbo.T.json");
            File.Exists(expectedPath).Should().BeTrue();

            // Sanity check: ensure we can read back contentHash similarly to production logic
            string? readHash = null;
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetIgnoreCase(root, "embedding", out var emb) &&
                        TryGetIgnoreCase(emb, "metadata", out var md) &&
                        TryGetIgnoreCase(md, "contentHash", out var ch))
                    {
                        readHash = ch.GetString();
                    }
                }
            }
            readHash.Should().Be("hash");

            var processed = await sut.GenerateAsync(
                model,
                tmp,
                new VectorGenerationOptions { Overwrite = false });
            processed.Should().Be(0);
            writer.Verify(
                w => w.UpsertAsync(
                    It.IsAny<GenAIDBExplorer.Core.SemanticVectors.Records.EntityVectorRecord>(),
                    It.IsAny<VectorInfrastructure>(),
                    default),
                Times.Never);
        }
        finally
        {
            try { tmp.Delete(true); }
            catch { }
        }
    }

    private static bool TryGetIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }
        foreach (var p in element.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }
        value = default;
        return false;
    }
}
