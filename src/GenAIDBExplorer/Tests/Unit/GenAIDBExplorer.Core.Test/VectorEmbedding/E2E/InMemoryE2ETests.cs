using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using GenAIDBExplorer.Core.SemanticVectors.Indexing;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Mapping;
using GenAIDBExplorer.Core.SemanticVectors.Orchestration;
using GenAIDBExplorer.Core.SemanticVectors.Search;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.VectorEmbedding.E2E;

[TestClass]
public class InMemoryE2ETests
{
    [TestMethod]
    public async Task Generate_Then_Search_InMemory_ShouldReturn_Result()
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), $"e2e-{Guid.NewGuid():N}");
        var tmp = new DirectoryInfo(tmpPath);
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
                VectorIndex = new VectorIndexSettings { Provider = "InMemory", CollectionName = "e2e", EmbeddingServiceId = "Embeddings" }
            };

            // Minimal infra
            var policy = new GenAIDBExplorer.Core.SemanticVectors.Policy.VectorIndexPolicy();
            var infraFactory = new VectorInfrastructureFactory(policy);
            var mapper = new VectorRecordMapper();
            var keyBuilder = new GenAIDBExplorer.Core.SemanticVectors.Keys.EntityKeyBuilder();

            // Deterministic embedding generator (mock) that returns fixed vector for given content
            var embed = new Moq.Mock<IEmbeddingGenerator>();
            embed
                .Setup(e => e.GenerateAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<VectorInfrastructure>(), default))
                .Returns((string content, VectorInfrastructure _, System.Threading.CancellationToken _) =>
                {
                    // Simple content hash based float vector of length 3
                    var sum = content.Sum(c => (int)c);
                    var arr = new float[] { sum % 10, (sum / 10) % 10, (sum / 100) % 10 };
                    return Task.FromResult((ReadOnlyMemory<float>)new ReadOnlyMemory<float>(arr));
                });

            var inMemory = new InMemoryVectorStore();
            var writer = new SkInMemoryVectorIndexWriter(
                inMemory,
                new GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor(
                    new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor>>()
                        .Object));
            var search = new SkInMemoryVectorSearchService(
                inMemory,
                new GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor(
                    new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor>>()
                        .Object));
            var serializer = new GenAIDBExplorer.Core.Repository.Security.SecureJsonSerializer(
                new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GenAIDBExplorer.Core.Repository.Security.SecureJsonSerializer>>()
                    .Object);
            var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<VectorGenerationService>>();
            var perf = new GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor(
                new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GenAIDBExplorer.Core.Repository.Performance.PerformanceMonitor>>()
                    .Object);
            var generator = new VectorGenerationService(
                proj,
                infraFactory,
                mapper,
                embed.Object,
                keyBuilder,
                writer,
                serializer,
                logger.Object,
                perf
            );

            var model = new SemanticModel("M", "S");
            var table = new SemanticModelTable("dbo", "E2E");
            model.AddTable(table);

            // Generate
            var processed = await generator.GenerateAsync(
                model,
                tmp,
                new VectorGenerationOptions { Overwrite = true });
            processed.Should().Be(1);

            // Search using same vector produced for content
            var content = mapper.BuildEntityText(table);
            var vector = await embed.Object.GenerateAsync(
                content,
                infraFactory.Create(proj.VectorIndex, proj.SemanticModel.PersistenceStrategy),
                default);
            var results = await search.SearchAsync(
                vector,
                topK: 1,
                infraFactory.Create(proj.VectorIndex, proj.SemanticModel.PersistenceStrategy));

            results.Should().NotBeEmpty();
            var first = results.First();
            first.Record.Schema.Should().Be("dbo");
            first.Record.Name.Should().Be("E2E");
            first.Score.Should().BeGreaterThan(0);
        }
        finally
        {
            try { tmp.Delete(true); }
            catch { }
        }
    }
}
