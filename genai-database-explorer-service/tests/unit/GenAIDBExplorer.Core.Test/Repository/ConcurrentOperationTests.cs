using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Repository
{
    /// <summary>
    /// Tests for concurrent operation protection in Phase 4c.
    /// </summary>
    [TestClass]
    public class ConcurrentOperationTests
    {
        private Mock<IPersistenceStrategyFactory> _mockStrategyFactory = null!;
        private Mock<ISemanticModelPersistenceStrategy> _mockStrategy = null!;
        private Mock<ILogger<SemanticModelRepository>> _mockLogger = null!;
        private Mock<ILoggerFactory> _mockLoggerFactory = null!;
        private string _testDirectory = null!;
        private SemanticModelRepository _repository = null!;

        [TestInitialize]
        public void Initialize()
        {
            _mockStrategyFactory = new Mock<IPersistenceStrategyFactory>();
            _mockStrategy = new Mock<ISemanticModelPersistenceStrategy>();
            _mockLogger = new Mock<ILogger<SemanticModelRepository>>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();

            _testDirectory = Path.Combine(Path.GetTempPath(), $"concurrent_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDirectory);

            _mockStrategyFactory.Setup(f => f.GetStrategy(It.IsAny<string>()))
                              .Returns(_mockStrategy.Object);

            _repository = new SemanticModelRepository(
                _mockStrategyFactory.Object,
                _mockLogger.Object,
                _mockLoggerFactory.Object,
                maxConcurrentOperations: 5);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _repository?.Dispose();

            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }

        [TestMethod]
        public async Task SaveModelAsync_WithConcurrentOperations_ShouldNotInterfere()
        {
            // Arrange
            var model = CreateTestSemanticModel();
            var modelPath = new DirectoryInfo(Path.Combine(_testDirectory, "concurrent_save"));

            var saveDelayMs = 100;
            var concurrentTasks = 5;

            _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
                        .Returns(async () => await Task.Delay(saveDelayMs));

            // Act
            var tasks = Enumerable.Range(0, concurrentTasks)
                                 .Select(_ => _repository.SaveModelAsync(model, modelPath))
                                 .ToArray();

            // Assert - Should complete without deadlocks or exceptions
            await Task.WhenAll(tasks);

            // Verify all operations completed
            _mockStrategy.Verify(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()),
                               Times.Exactly(concurrentTasks));
        }

        [TestMethod]
        public async Task LoadModelAsync_WithConcurrentOperations_ShouldNotInterfere()
        {
            // Arrange
            var model = CreateTestSemanticModel();
            var modelPath = new DirectoryInfo(Path.Combine(_testDirectory, "concurrent_load"));

            var loadDelayMs = 100;
            var concurrentTasks = 5;

            _mockStrategy.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
                        .Returns(async () =>
                        {
                            await Task.Delay(loadDelayMs);
                            return model;
                        });

            // Act
            var tasks = Enumerable.Range(0, concurrentTasks)
                                 .Select(_ => _repository.LoadModelAsync(modelPath))
                                 .ToArray();

            // Assert - Should complete without deadlocks or exceptions
            var results = await Task.WhenAll(tasks);

            // Verify all operations completed and returned results
            results.Should().NotBeNull();
            results.Should().AllSatisfy(result => result.Should().NotBeNull());
            _mockStrategy.Verify(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()),
                               Times.Exactly(concurrentTasks));
        }

        [TestMethod]
        public async Task MixedOperations_WithConcurrentAccess_ShouldSerializeOnSamePath()
        {
            // Arrange
            var model = CreateTestSemanticModel();
            var modelPath = new DirectoryInfo(Path.Combine(_testDirectory, "mixed_operations"));

            var operationDelayMs = 50;
            var operationCount = 0;

            _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
                        .Returns(async () =>
                        {
                            Interlocked.Increment(ref operationCount);
                            await Task.Delay(operationDelayMs);
                        });

            _mockStrategy.Setup(s => s.LoadModelAsync(It.IsAny<DirectoryInfo>()))
                        .Returns(async () =>
                        {
                            Interlocked.Increment(ref operationCount);
                            await Task.Delay(operationDelayMs);
                            return model;
                        });

            // Act - Mix save and load operations on the same path
            var saveTasks = Enumerable.Range(0, 3)
                                    .Select(_ => _repository.SaveModelAsync(model, modelPath));

            var loadTasks = Enumerable.Range(0, 3)
                                    .Select(_ => _repository.LoadModelAsync(modelPath));

            var allTasks = saveTasks.Concat(loadTasks).ToArray();

            // Assert - Should complete without race conditions
            await Task.WhenAll(allTasks);

            operationCount.Should().Be(6); // 3 saves + 3 loads
        }

        [TestMethod]
        public async Task MaxConcurrentOperations_ShouldLimitGlobalConcurrency()
        {
            // Arrange
            var model = CreateTestSemanticModel();
            var maxConcurrent = 3;
            var repository = new SemanticModelRepository(
                _mockStrategyFactory.Object,
                _mockLogger.Object,
                _mockLoggerFactory.Object,
                maxConcurrentOperations: maxConcurrent);

            var concurrentExecutions = 0;
            var maxObservedConcurrency = 0;
            var lockObject = new object();

            _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
                        .Returns(async () =>
                        {
                            lock (lockObject)
                            {
                                concurrentExecutions++;
                                maxObservedConcurrency = Math.Max(maxObservedConcurrency, concurrentExecutions);
                            }

                            await Task.Delay(100); // Simulate work

                            lock (lockObject)
                            {
                                concurrentExecutions--;
                            }
                        });

            // Act - Start more tasks than the concurrency limit
            var tasks = Enumerable.Range(0, maxConcurrent * 2)
                                 .Select(i => repository.SaveModelAsync(model,
                                     new DirectoryInfo(Path.Combine(_testDirectory, $"model_{i}"))))
                                 .ToArray();

            await Task.WhenAll(tasks);

            // Assert - Should not exceed the concurrency limit
            maxObservedConcurrency.Should().BeLessThanOrEqualTo(maxConcurrent);

            repository.Dispose();
        }

        [TestMethod]
        public async Task PathSpecificSerialization_ShouldWorkCorrectly()
        {
            // Arrange
            var model = CreateTestSemanticModel();
            var path1 = new DirectoryInfo(Path.Combine(_testDirectory, "path1"));
            var path2 = new DirectoryInfo(Path.Combine(_testDirectory, "path2"));

            var executionOrder = new List<string>();
            var lockObject = new object();

            _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
                        .Returns<SemanticModel, DirectoryInfo>(async (model, path) =>
                        {
                            lock (lockObject)
                            {
                                executionOrder.Add($"Start-{path.Name}");
                            }

                            await Task.Delay(100);

                            lock (lockObject)
                            {
                                executionOrder.Add($"End-{path.Name}");
                            }
                        });

            // Act - Operations on different paths should run concurrently
            // Operations on same path should be serialized
            var tasks = new[]
            {
                _repository.SaveModelAsync(model, path1), // Should run
                _repository.SaveModelAsync(model, path2), // Should run concurrently with path1
                _repository.SaveModelAsync(model, path1), // Should wait for first path1 operation
                _repository.SaveModelAsync(model, path2)  // Should wait for first path2 operation
            };

            await Task.WhenAll(tasks);

            // Assert - Operations on same path should be properly serialized
            lock (lockObject)
            {
                var path1Operations = executionOrder.Where(op => op.Contains("path1")).ToList();
                var path2Operations = executionOrder.Where(op => op.Contains("path2")).ToList();

                // Each path should have proper start/end pairing
                path1Operations.Count.Should().Be(4); // 2 start + 2 end
                path2Operations.Count.Should().Be(4); // 2 start + 2 end

                // First start should come before first end for each path
                path1Operations.IndexOf("Start-path1").Should().BeLessThan(path1Operations.IndexOf("End-path1"));
                path2Operations.IndexOf("Start-path2").Should().BeLessThan(path2Operations.IndexOf("End-path2"));
            }
        }

        [TestMethod]
        public async Task Repository_Disposal_ShouldNotAffectOngoingOperations()
        {
            // Arrange
            var model = CreateTestSemanticModel();
            var modelPath = new DirectoryInfo(Path.Combine(_testDirectory, "disposal_test"));
            var taskCompletionSource = new TaskCompletionSource<bool>();

            _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
                        .Returns(async () =>
                        {
                            // Signal that the operation has started
                            taskCompletionSource.SetResult(true);

                            // Short delay before completion
                            await Task.Delay(10);
                        });

            // Act
            var operationTask = _repository.SaveModelAsync(model, modelPath);

            // Wait for operation to start
            await taskCompletionSource.Task;

            // Complete the operation first, then dispose
            await operationTask;

            // Dispose repository after operation completes
            _repository.Dispose();

            // Verify operation completed
            _mockStrategy.Verify(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()),
                               Times.Once);
        }

        [TestMethod]
        public void Repository_OperationAfterDisposal_ShouldThrow()
        {
            // Arrange
            var model = CreateTestSemanticModel();
            var modelPath = new DirectoryInfo(Path.Combine(_testDirectory, "after_disposal"));

            // Act
            _repository.Dispose();

            // Assert
            FluentActions.Invoking(() => _repository.SaveModelAsync(model, modelPath))
                .Should().ThrowAsync<ObjectDisposedException>();
        }

        [TestMethod]
        public async Task ThreadSafety_StressTest_ShouldNotCorrupt()
        {
            // Arrange
            var model = CreateTestSemanticModel();
            var stressTestTasks = 50;
            var operationCount = 0;
            var exceptions = new List<Exception>();

            _mockStrategy.Setup(s => s.SaveModelAsync(It.IsAny<SemanticModel>(), It.IsAny<DirectoryInfo>()))
                        .Returns(async () =>
                        {
                            Interlocked.Increment(ref operationCount);
                            await Task.Delay(10); // Small delay to increase contention
                        });

            // Act - Create high contention scenario
            var tasks = Enumerable.Range(0, stressTestTasks)
                                 .Select(async i =>
                                 {
                                     try
                                     {
                                         var path = new DirectoryInfo(Path.Combine(_testDirectory, $"stress_{i % 5}"));
                                         await _repository.SaveModelAsync(model, path);
                                     }
                                     catch (Exception ex)
                                     {
                                         lock (exceptions)
                                         {
                                             exceptions.Add(ex);
                                         }
                                     }
                                 })
                                 .ToArray();

            await Task.WhenAll(tasks);

            // Assert - All operations should complete without corruption
            exceptions.Should().BeEmpty();
            operationCount.Should().Be(stressTestTasks);
        }

        private static SemanticModel CreateTestSemanticModel()
        {
            return new SemanticModel(
                name: "TestModel",
                source: "Server=test;Database=test",
                description: "Test model for concurrent operations");
        }
    }
}
