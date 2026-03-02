using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticModelQuery;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using GenAIDBExplorer.Core.SemanticVectors.Search;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.SemanticModelQuery;

[TestClass]
public class SemanticModelSearchServiceTests
{
    private Mock<IEmbeddingGenerator> _mockEmbeddingGenerator = null!;
    private Mock<IVectorSearchService> _mockVectorSearchService = null!;
    private Mock<IVectorInfrastructureFactory> _mockInfrastructureFactory = null!;
    private Mock<IProject> _mockProject = null!;
    private Mock<ILogger<SemanticModelSearchService>> _mockLogger = null!;
    private SemanticModelSearchService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator>();
        _mockVectorSearchService = new Mock<IVectorSearchService>();
        _mockInfrastructureFactory = new Mock<IVectorInfrastructureFactory>();
        _mockProject = new Mock<IProject>();
        _mockLogger = new Mock<ILogger<SemanticModelSearchService>>();

        var settings = CreateProjectSettings();
        _mockProject.Setup(p => p.Settings).Returns(settings);

        var infrastructure = CreateInfrastructure();
        _mockInfrastructureFactory
            .Setup(f => f.Create(It.IsAny<VectorIndexSettings>(), It.IsAny<string>()))
            .Returns(infrastructure);

        _service = new SemanticModelSearchService(
            _mockEmbeddingGenerator.Object,
            _mockVectorSearchService.Object,
            _mockInfrastructureFactory.Object,
            _mockProject.Object,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task SearchTablesAsync_WithResults_ShouldReturnFilteredTableResults()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f, 2.0f, 3.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var records = new List<(EntityVectorRecord Record, double Score)>
        {
            (CreateRecord("Table", "SalesLT", "Customer", "Customer table"), 0.95),
            (CreateRecord("View", "SalesLT", "vCustomer", "Customer view"), 0.90),
            (CreateRecord("Table", "SalesLT", "Order", "Order table"), 0.85),
        };
        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var results = await _service.SearchTablesAsync("customer data", 5);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.EntityType.Should().Be("Table"));
        results[0].EntityName.Should().Be("Customer");
        results[0].Score.Should().Be(0.95);
    }

    [TestMethod]
    public async Task SearchViewsAsync_WithResults_ShouldReturnFilteredViewResults()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f, 2.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var records = new List<(EntityVectorRecord Record, double Score)>
        {
            (CreateRecord("Table", "SalesLT", "Customer", "Customer table"), 0.95),
            (CreateRecord("View", "SalesLT", "vCustomer", "Customer view"), 0.90),
        };
        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var results = await _service.SearchViewsAsync("customer view", 5);

        // Assert
        results.Should().HaveCount(1);
        results[0].EntityType.Should().Be("View");
        results[0].EntityName.Should().Be("vCustomer");
    }

    [TestMethod]
    public async Task SearchStoredProceduresAsync_WithResults_ShouldReturnFilteredResults()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var records = new List<(EntityVectorRecord Record, double Score)>
        {
            (CreateRecord("StoredProcedure", "dbo", "uspGetOrders", "Get orders proc"), 0.88),
        };
        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var results = await _service.SearchStoredProceduresAsync("order processing", 5);

        // Assert
        results.Should().HaveCount(1);
        results[0].EntityType.Should().Be("StoredProcedure");
        results[0].SchemaName.Should().Be("dbo");
    }

    [TestMethod]
    public async Task SearchTablesAsync_WithEmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<(EntityVectorRecord, double)>());

        // Act
        var results = await _service.SearchTablesAsync("nonexistent", 5);

        // Assert
        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SearchTablesAsync_ShouldCallEmbeddingGenerator()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<(EntityVectorRecord, double)>());

        // Act
        await _service.SearchTablesAsync("test query", 5);

        // Assert
        _mockEmbeddingGenerator.Verify(
            e => e.GenerateAsync("test query", It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchTablesAsync_ShouldOverFetchByMultiplier()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<(EntityVectorRecord, double)>());

        // Act
        await _service.SearchTablesAsync("test", 5);

        // Assert — should over-fetch by 3x (5 * 3 = 15)
        _mockVectorSearchService.Verify(
            s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), 15, It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchAsync_ValidQuery_ReturnsRankedResults()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f, 2.0f, 3.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var records = new List<(EntityVectorRecord Record, double Score)>
        {
            (CreateRecord("Table", "SalesLT", "Customer", "Customer table"), 0.95),
            (CreateRecord("View", "SalesLT", "vOrders", "Orders view"), 0.82),
            (CreateRecord("StoredProcedure", "dbo", "uspGetCustomer", "Get customer proc"), 0.65),
        };
        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var results = await _service.SearchAsync("customer orders", 5, null);

        // Assert
        results.Should().HaveCount(3);
        results[0].Score.Should().Be(0.95);
        results[1].Score.Should().Be(0.82);
        results[2].Score.Should().Be(0.65);
    }

    [TestMethod]
    public async Task SearchAsync_EmptyVectorStore_ReturnsEmptyList()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<(EntityVectorRecord, double)>());

        // Act
        var results = await _service.SearchAsync("nonexistent", 5, null);

        // Assert
        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SearchAsync_ScoreThreshold_FiltersLowRelevanceResults()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var records = new List<(EntityVectorRecord Record, double Score)>
        {
            (CreateRecord("Table", "dbo", "HighScore", "High relevance"), 0.80),
            (CreateRecord("Table", "dbo", "LowScore", "Low relevance"), 0.20),
            (CreateRecord("Table", "dbo", "MidScore", "Medium relevance"), 0.50),
        };
        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var results = await _service.SearchAsync("test query", 5, null);

        // Assert — LowScore (0.20) should be filtered out by the 0.3 threshold
        results.Should().HaveCount(2);
        results.Should().NotContain(r => r.EntityName == "LowScore");
    }

    [TestMethod]
    public async Task SearchAsync_TopKCappedAtMaximum_ReturnsOnlyMaxResults()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var records = Enumerable.Range(1, 12)
            .Select(i => (CreateRecord("Table", "dbo", $"Table{i}", $"Table {i}"), 0.9 - (i * 0.01)))
            .ToList<(EntityVectorRecord, double)>();
        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act — request topK=15 which exceeds the maximum of 10
        var results = await _service.SearchAsync("test", 15, null);

        // Assert
        results.Count.Should().BeLessThanOrEqualTo(10);
    }

    [TestMethod]
    public async Task SearchAsync_SingleEmbeddingGenerationCall()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<(EntityVectorRecord, double)>());

        // Act
        await _service.SearchAsync("test query", 5, null);

        // Assert — exactly one embedding generation call regardless of entity types
        _mockEmbeddingGenerator.Verify(
            e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchAsync_WithEntityTypeFilter_ReturnsOnlyMatchingTypes()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>([1.0f]);
        _mockEmbeddingGenerator
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var records = new List<(EntityVectorRecord Record, double Score)>
        {
            (CreateRecord("Table", "SalesLT", "Customer", "Customer table"), 0.95),
            (CreateRecord("View", "SalesLT", "vCustomer", "Customer view"), 0.90),
            (CreateRecord("StoredProcedure", "dbo", "uspGetCustomer", "Get customer"), 0.85),
        };
        _mockVectorSearchService
            .Setup(s => s.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<VectorInfrastructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var results = await _service.SearchAsync("customer", 5, ["Table"]);

        // Assert
        results.Should().HaveCount(1);
        results.Should().AllSatisfy(r => r.EntityType.Should().Be("Table"));
    }

    private static EntityVectorRecord CreateRecord(string entityType, string schema, string name, string content)
    {
        return new EntityVectorRecord
        {
            Id = $"{schema}.{name}",
            Content = content,
            EntityType = entityType,
            Schema = schema,
            Name = name
        };
    }

    private static VectorInfrastructure CreateInfrastructure()
    {
        return new VectorInfrastructure(
            Provider: "InMemory",
            CollectionName: "test-collection",
            EmbeddingServiceId: "Embeddings",
            Settings: new VectorIndexSettings());
    }

    private static ProjectSettings CreateProjectSettings()
    {
        return new ProjectSettings
        {
            SettingsVersion = new Version("2.0.0"),
            Database = new DatabaseSettings { Name = "TestDB", Description = "Test database" },
            DataDictionary = new DataDictionarySettings(),
            MicrosoftFoundry = new MicrosoftFoundrySettings(),
            SemanticModel = new SemanticModelSettings { PersistenceStrategy = "LocalDisk" },
            SemanticModelRepository = new SemanticModelRepositorySettings(),
            VectorIndex = new VectorIndexSettings(),
            QueryModel = new QueryModelSettings()
        };
    }
}
