using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository;

/// <summary>
/// Integration tests for SemanticModelRepository with immutable builder pattern support.
/// Tests the new LoadModelAsync overload that accepts SemanticModelRepositoryOptions.
/// </summary>
[TestClass]
public class SemanticModelRepositoryBuilderIntegrationTests
{
    private Mock<IPersistenceStrategyFactory> _mockStrategyFactory = null!;
    private Mock<ISemanticModelPersistenceStrategy> _mockStrategy = null!;
    private Mock<ILogger<SemanticModelRepository>> _mockLogger = null!;
    private SemanticModelRepository _repository = null!;
    private DirectoryInfo _testModelPath = null!;
    private SemanticModel _testModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockStrategyFactory = new Mock<IPersistenceStrategyFactory>();
        _mockStrategy = new Mock<ISemanticModelPersistenceStrategy>();
        _mockLogger = new Mock<ILogger<SemanticModelRepository>>();

        _mockStrategyFactory.Setup(f => f.GetStrategy(It.IsAny<string?>()))
            .Returns(_mockStrategy.Object);

        _repository = new SemanticModelRepository(
            _mockStrategyFactory.Object,
            _mockLogger.Object);

        _testModelPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "test-model"));
        _testModel = new SemanticModel("TestDatabase", "Test Database", null)
        {
            Tables = [],
            Views = [],
            StoredProcedures = []
        };

        _mockStrategy.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
            .ReturnsAsync(_testModel);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _repository?.Dispose();
    }

    [TestMethod]
    public async Task LoadModelAsync_WithOptionsBuilder_ShouldCallExistingOverload()
    {
        // Arrange
        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading(true)
            .WithChangeTracking(true)
            .WithStrategyName("TestStrategy")
            .Build();

        // Act
        var result = await _repository.LoadModelAsync(_testModelPath, options);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_testModel);

        // Verify that the strategy factory was called with the correct strategy name
        _mockStrategyFactory.Verify(f => f.GetStrategy("TestStrategy"), Times.Once);
        _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        SemanticModelRepositoryOptions? nullOptions = null;

        // Act & Assert
        var act = async () => await _repository.LoadModelAsync(_testModelPath, nullOptions!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*options*");
    }

    [TestMethod]
    public async Task LoadModelAsync_WithOptionsBuilder_AllFeaturesEnabled_ShouldPassCorrectParameters()
    {
        // Arrange
        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading(true)
            .WithChangeTracking(true)
            .WithCaching(true)
            .WithStrategyName("AzureBlob")
            .Build();

        // Act
        var result = await _repository.LoadModelAsync(_testModelPath, options);

        // Assert
        result.Should().NotBeNull();
        _mockStrategyFactory.Verify(f => f.GetStrategy("AzureBlob"), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithOptionsBuilder_DefaultValues_ShouldUseDefaults()
    {
        // Arrange
        var options = SemanticModelRepositoryOptionsBuilder.Create().Build();

        // Act
        var result = await _repository.LoadModelAsync(_testModelPath, options);

        // Assert
        result.Should().NotBeNull();
        
        // Should use default strategy (null strategy name)
        _mockStrategyFactory.Verify(f => f.GetStrategy(null), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithOptionsBuilder_ComplexConfiguration_ShouldWork()
    {
        // Arrange
        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading(true)
            .WithChangeTracking(false)
            .WithCaching(true, TimeSpan.FromMinutes(30))
            .WithStrategyName("Cosmos")
            .WithMaxConcurrentOperations(5)
            .WithPerformanceMonitoring(perf => perf
                .EnableLocalMonitoring(true)
                .WithMetricsRetention(TimeSpan.FromHours(12)))
            .Build();

        // Act
        var result = await _repository.LoadModelAsync(_testModelPath, options);

        // Assert
        result.Should().NotBeNull();
        
        // Verify options were correctly configured
        options.EnableLazyLoading.Should().BeTrue();
        options.EnableChangeTracking.Should().BeFalse();
        options.EnableCaching.Should().BeTrue();
        options.CacheExpiration.Should().Be(TimeSpan.FromMinutes(30));
        options.StrategyName.Should().Be("Cosmos");
        options.MaxConcurrentOperations.Should().Be(5);
        options.PerformanceMonitoring.Should().NotBeNull();
        options.PerformanceMonitoring!.EnableLocalMonitoring.Should().BeTrue();
        options.PerformanceMonitoring.MetricsRetentionPeriod.Should().Be(TimeSpan.FromHours(12));
        
        _mockStrategyFactory.Verify(f => f.GetStrategy("Cosmos"), Times.Once);
    }

    [TestMethod]
    public async Task LoadModelAsync_WithOptionsBuilder_ThreadSafeUsage_ShouldWorkConcurrently()
    {
        // Arrange
        var baseOptions = SemanticModelRepositoryOptionsBuilder.Create();
        var tasks = new List<Task<SemanticModel>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(async () =>
            {
                var options = baseOptions
                    .WithLazyLoading(threadId % 2 == 0)
                    .WithStrategyName($"Strategy{threadId}")
                    .Build();

                return await _repository.LoadModelAsync(_testModelPath, options);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(result => result.Should().NotBeNull());
        
        // Verify that each thread called with its own strategy
        for (int i = 0; i < 10; i++)
        {
            _mockStrategyFactory.Verify(f => f.GetStrategy($"Strategy{i}"), Times.Once);
        }
    }

    [TestMethod]
    public void OptionsImmutability_BuiltOptionsCannotBeModified_ShouldBeImmutable()
    {
        // Arrange
        var options = SemanticModelRepositoryOptionsBuilder.Create()
            .WithLazyLoading(true)
            .WithChangeTracking(true)
            .Build();

        // Act & Assert
        // Since we're using records with init properties, attempting to modify
        // should result in compilation errors. This test documents the immutable nature.
        options.EnableLazyLoading.Should().BeTrue();
        options.EnableChangeTracking.Should().BeTrue();
        
        // The following would not compile, demonstrating immutability:
        // options.EnableLazyLoading = false; // Compilation error
        // options.EnableChangeTracking = false; // Compilation error
    }

    [TestMethod]
    public void BuilderReuse_StaticFieldPattern_ShouldBeSafeForConcurrentUse()
    {
        // Arrange
        // This test demonstrates the safe static field pattern we discussed
        var staticBaseBuilder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var developmentOptions = staticBaseBuilder
            .WithLazyLoading(true)
            .WithChangeTracking(true)
            .Build();

        var productionOptions = staticBaseBuilder
            .WithLazyLoading(false)
            .WithCaching(true)
            .Build();

        // Assert
        // Each usage of the static builder should produce independent results
        developmentOptions.EnableLazyLoading.Should().BeTrue();
        developmentOptions.EnableChangeTracking.Should().BeTrue();
        developmentOptions.EnableCaching.Should().BeFalse();

        productionOptions.EnableLazyLoading.Should().BeFalse();
        productionOptions.EnableChangeTracking.Should().BeFalse();
        productionOptions.EnableCaching.Should().BeTrue();
    }
}
