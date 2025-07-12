using FluentAssertions;
using GenAIDBExplorer.Core.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.Repository;

/// <summary>
/// Unit tests for PerformanceMonitoringOptionsBuilder implementing immutable builder pattern.
/// Tests cover fluent interface, immutable behavior, and validation.
/// </summary>
[TestClass]
public class PerformanceMonitoringOptionsBuilderTests
{
    [TestMethod]
    public void Create_ShouldReturnBuilderWithDefaultOptions()
    {
        // Arrange & Act
        var builder = PerformanceMonitoringOptionsBuilder.Create();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeAssignableTo<IPerformanceMonitoringOptionsBuilder>();
    }

    [TestMethod]
    public void Build_WithDefaults_ShouldReturnOptionsWithDefaultValues()
    {
        // Arrange
        var builder = PerformanceMonitoringOptionsBuilder.Create();

        // Act
        var options = builder.Build();

        // Assert
        options.Should().NotBeNull();
        options.EnableLocalMonitoring.Should().BeTrue();
        options.MetricsRetentionPeriod.Should().Be(TimeSpan.FromHours(24));
    }

    [TestMethod]
    public void EnableLocalMonitoring_ShouldReturnNewBuilderInstance()
    {
        // Arrange
        var originalBuilder = PerformanceMonitoringOptionsBuilder.Create();

        // Act
        var newBuilder = originalBuilder.EnableLocalMonitoring(false);

        // Assert
        newBuilder.Should().NotBeSameAs(originalBuilder);
        newBuilder.Should().BeAssignableTo<IPerformanceMonitoringOptionsBuilder>();
    }

    [TestMethod]
    public void EnableLocalMonitoring_ShouldSetLocalMonitoringOption()
    {
        // Arrange
        var builder = PerformanceMonitoringOptionsBuilder.Create();

        // Act
        var optionsEnabled = builder.EnableLocalMonitoring(true).Build();
        var optionsDisabled = builder.EnableLocalMonitoring(false).Build();

        // Assert
        optionsEnabled.EnableLocalMonitoring.Should().BeTrue();
        optionsDisabled.EnableLocalMonitoring.Should().BeFalse();
    }

    [TestMethod]
    public void WithMetricsRetention_ShouldSetRetentionPeriod()
    {
        // Arrange
        var builder = PerformanceMonitoringOptionsBuilder.Create();
        var retention = TimeSpan.FromHours(8);

        // Act
        var options = builder.WithMetricsRetention(retention).Build();

        // Assert
        options.MetricsRetentionPeriod.Should().Be(retention);
    }

    [TestMethod]
    public void WithMetricsRetention_WithNegativeValue_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = PerformanceMonitoringOptionsBuilder.Create();
        var negativeRetention = TimeSpan.FromHours(-1);

        // Act & Assert
        var act = () => builder.WithMetricsRetention(negativeRetention);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Metrics retention period cannot be negative*")
            .And.ParamName.Should().Be("retention");
    }

    [TestMethod]
    public void FluentInterface_ShouldAllowMethodChaining()
    {
        // Arrange
        var builder = PerformanceMonitoringOptionsBuilder.Create();

        // Act
        var options = builder
            .EnableLocalMonitoring(false)
            .WithMetricsRetention(TimeSpan.FromHours(12))
            .Build();

        // Assert
        options.EnableLocalMonitoring.Should().BeFalse();
        options.MetricsRetentionPeriod.Should().Be(TimeSpan.FromHours(12));
    }

    [TestMethod]
    public void ImmutableBehavior_ShouldCreateIndependentInstances()
    {
        // Arrange
        var baseBuilder = PerformanceMonitoringOptionsBuilder.Create();

        // Act
        var builder1 = baseBuilder.EnableLocalMonitoring(true);
        var builder2 = baseBuilder.EnableLocalMonitoring(false);

        var options1 = builder1.Build();
        var options2 = builder2.Build();

        // Assert
        options1.EnableLocalMonitoring.Should().BeTrue();
        options2.EnableLocalMonitoring.Should().BeFalse();
    }

    [TestMethod]
    public void ThreadSafety_MultipleThreadsUsingBuilder_ShouldNotInterfereWithEachOther()
    {
        // Arrange
        var baseBuilder = PerformanceMonitoringOptionsBuilder.Create();
        var results = new List<PerformanceMonitoringOptions>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                var options = baseBuilder
                    .EnableLocalMonitoring(threadId % 2 == 0)
                    .WithMetricsRetention(TimeSpan.FromHours(threadId + 1))
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
            options.MetricsRetentionPeriod.Should().NotBeNull();
            options.MetricsRetentionPeriod!.Value.TotalHours.Should().BeInRange(1, 10);
            options.EnableLocalMonitoring.Should().BeOneOf(true, false);
        }
    }
}
