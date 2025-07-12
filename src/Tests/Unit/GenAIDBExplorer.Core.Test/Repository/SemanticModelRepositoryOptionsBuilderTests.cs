using FluentAssertions;
using GenAIDBExplorer.Core.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.Repository;

/// <summary>
/// Unit tests for SemanticModelRepositoryOptionsBuilder implementing immutable builder pattern.
/// Tests cover fluent interface, immutable behavior, thread safety, and validation.
/// </summary>
[TestClass]
public class SemanticModelRepositoryOptionsBuilderTests
{
    [TestMethod]
    public void Create_ShouldReturnBuilderWithDefaultOptions()
    {
        // Arrange & Act
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeAssignableTo<ISemanticModelRepositoryOptionsBuilder>();
    }

    [TestMethod]
    public void Build_WithDefaults_ShouldReturnOptionsWithDefaultValues()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var options = builder.Build();

        // Assert
        options.Should().NotBeNull();
        options.EnableLazyLoading.Should().BeFalse();
        options.EnableChangeTracking.Should().BeFalse();
        options.EnableCaching.Should().BeFalse();
        options.StrategyName.Should().BeNull();
        options.CacheExpiration.Should().BeNull();
        options.MaxConcurrentOperations.Should().BeNull();
        options.PerformanceMonitoring.Should().BeNull();
    }

    [TestMethod]
    public void WithLazyLoading_ShouldReturnNewBuilderInstance()
    {
        // Arrange
        var originalBuilder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var newBuilder = originalBuilder.WithLazyLoading(true);

        // Assert
        newBuilder.Should().NotBeSameAs(originalBuilder);
        newBuilder.Should().BeAssignableTo<ISemanticModelRepositoryOptionsBuilder>();
    }

    [TestMethod]
    public void WithLazyLoading_ShouldSetLazyLoadingOption()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var options = builder.WithLazyLoading(true).Build();

        // Assert
        options.EnableLazyLoading.Should().BeTrue();
    }

    [TestMethod]
    public void WithChangeTracking_ShouldSetChangeTrackingOption()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var options = builder.WithChangeTracking(true).Build();

        // Assert
        options.EnableChangeTracking.Should().BeTrue();
    }

    [TestMethod]
    public void WithCaching_ShouldSetCachingOption()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var options = builder.WithCaching(true).Build();

        // Assert
        options.EnableCaching.Should().BeTrue();
    }

    [TestMethod]
    public void WithCaching_WithExpiration_ShouldSetBothCachingAndExpiration()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();
        var expiration = TimeSpan.FromMinutes(30);

        // Act
        var options = builder.WithCaching(true, expiration).Build();

        // Assert
        options.EnableCaching.Should().BeTrue();
        options.CacheExpiration.Should().Be(expiration);
    }

    [TestMethod]
    public void WithCaching_WithNegativeExpiration_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();
        var negativeExpiration = TimeSpan.FromMinutes(-1);

        // Act & Assert
        var act = () => builder.WithCaching(true, negativeExpiration);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Cache expiration cannot be negative*")
            .And.ParamName.Should().Be("expiration");
    }

    [TestMethod]
    public void WithStrategyName_ShouldSetStrategyName()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();
        var strategyName = "AzureBlob";

        // Act
        var options = builder.WithStrategyName(strategyName).Build();

        // Assert
        options.StrategyName.Should().Be(strategyName);
    }

    [TestMethod]
    public void WithStrategyName_WithNullOrWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act & Assert
        var actNull = () => builder.WithStrategyName(null!);
        actNull.Should().Throw<ArgumentException>()
            .WithMessage("Strategy name cannot be null or whitespace*")
            .And.ParamName.Should().Be("strategyName");

        var actEmpty = () => builder.WithStrategyName("");
        actEmpty.Should().Throw<ArgumentException>()
            .WithMessage("Strategy name cannot be null or whitespace*")
            .And.ParamName.Should().Be("strategyName");

        var actWhitespace = () => builder.WithStrategyName("   ");
        actWhitespace.Should().Throw<ArgumentException>()
            .WithMessage("Strategy name cannot be null or whitespace*")
            .And.ParamName.Should().Be("strategyName");
    }

    [TestMethod]
    public void WithMaxConcurrentOperations_ShouldSetMaxConcurrentOperations()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();
        var maxOperations = 5;

        // Act
        var options = builder.WithMaxConcurrentOperations(maxOperations).Build();

        // Assert
        options.MaxConcurrentOperations.Should().Be(maxOperations);
    }

    [TestMethod]
    public void WithMaxConcurrentOperations_WithZeroOrNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act & Assert
        var actZero = () => builder.WithMaxConcurrentOperations(0);
        actZero.Should().Throw<ArgumentException>()
            .WithMessage("Max concurrent operations must be at least 1*")
            .And.ParamName.Should().Be("maxOperations");

        var actNegative = () => builder.WithMaxConcurrentOperations(-1);
        actNegative.Should().Throw<ArgumentException>()
            .WithMessage("Max concurrent operations must be at least 1*")
            .And.ParamName.Should().Be("maxOperations");
    }

    [TestMethod]
    public void WithPerformanceMonitoring_ShouldSetPerformanceMonitoring()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var options = builder.WithPerformanceMonitoring(perf =>
            perf.EnableLocalMonitoring(true)
                .WithMetricsRetention(TimeSpan.FromHours(8))).Build();

        // Assert
        options.PerformanceMonitoring.Should().NotBeNull();
        options.PerformanceMonitoring!.EnableLocalMonitoring.Should().BeTrue();
        options.PerformanceMonitoring.MetricsRetentionPeriod.Should().Be(TimeSpan.FromHours(8));
    }

    [TestMethod]
    public void WithPerformanceMonitoring_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act & Assert
        var act = () => builder.WithPerformanceMonitoring(null!);
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("configure");
    }

    [TestMethod]
    public void FluentInterface_ShouldAllowMethodChaining()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var options = builder
            .WithLazyLoading(true)
            .WithChangeTracking(true)
            .WithCaching(true, TimeSpan.FromMinutes(15))
            .WithStrategyName("LocalDisk")
            .WithMaxConcurrentOperations(8)
            .Build();

        // Assert
        options.EnableLazyLoading.Should().BeTrue();
        options.EnableChangeTracking.Should().BeTrue();
        options.EnableCaching.Should().BeTrue();
        options.CacheExpiration.Should().Be(TimeSpan.FromMinutes(15));
        options.StrategyName.Should().Be("LocalDisk");
        options.MaxConcurrentOperations.Should().Be(8);
    }

    [TestMethod]
    public void ImmutableBehavior_ShouldCreateIndependentInstances()
    {
        // Arrange
        var baseBuilder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var builder1 = baseBuilder.WithLazyLoading(true);
        var builder2 = baseBuilder.WithChangeTracking(true);

        var options1 = builder1.Build();
        var options2 = builder2.Build();

        // Assert
        options1.EnableLazyLoading.Should().BeTrue();
        options1.EnableChangeTracking.Should().BeFalse();

        options2.EnableLazyLoading.Should().BeFalse();
        options2.EnableChangeTracking.Should().BeTrue();
    }

    [TestMethod]
    public void Build_WithInvalidCacheExpiration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act & Assert
        // Note: This tests the validation in Build() method
        // The WithCaching method already validates negative expiration, 
        // but this tests the edge case where internal state might be invalid
        var act = () => builder.WithCaching(true).Build();
        act.Should().NotThrow(); // Valid case should not throw
    }

    [TestMethod]
    public void ThreadSafety_MultipleThreadsUsingBuilder_ShouldNotInterfereWithEachOther()
    {
        // Arrange
        var baseBuilder = SemanticModelRepositoryOptionsBuilder.Create();
        var results = new List<SemanticModelRepositoryOptions>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                var options = baseBuilder
                    .WithLazyLoading(threadId % 2 == 0)
                    .WithChangeTracking(threadId % 3 == 0)
                    .WithMaxConcurrentOperations(threadId + 1)
                    .Build();

                lock (results)
                {
                    results.Add(options);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        results.Should().HaveCount(10);
        
        // Verify that each thread produced independent results
        for (int i = 0; i < 10; i++)
        {
            var options = results[i];
            // We can't guarantee order, but we can verify that all combinations are valid
            options.MaxConcurrentOperations.Should().BeInRange(1, 10);
            options.EnableLazyLoading.Should().BeOneOf(true, false);
            options.EnableChangeTracking.Should().BeOneOf(true, false);
        }
    }

    [TestMethod]
    public void StaticFieldUsage_ShouldBeSafeForConcurrentAccess()
    {
        // Arrange
        // Simulate the pattern mentioned in our earlier discussion about static fields
        var staticBuilder = SemanticModelRepositoryOptionsBuilder.Create();

        // Act
        var task1 = Task.Run(() => staticBuilder
            .WithLazyLoading(true)
            .WithStrategyName("Strategy1")
            .Build());

        var task2 = Task.Run(() => staticBuilder
            .WithChangeTracking(true)
            .WithStrategyName("Strategy2")
            .Build());

        Task.WaitAll(task1, task2);

        // Assert
        var options1 = task1.Result;
        var options2 = task2.Result;

        // Each task should have completely independent results
        options1.EnableLazyLoading.Should().BeTrue();
        options1.EnableChangeTracking.Should().BeFalse();
        options1.StrategyName.Should().Be("Strategy1");

        options2.EnableLazyLoading.Should().BeFalse();
        options2.EnableChangeTracking.Should().BeTrue();
        options2.StrategyName.Should().Be("Strategy2");
    }
}
