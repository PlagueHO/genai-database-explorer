using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.Extensions.Logging;
using Azure.Core;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.Repository
{
    [TestClass]
    public class AzureBlobPersistenceStrategyTests
    {
        [TestMethod]
        public async Task SaveBlobAsync_RetriesOnTransientFailure()
        {
            // Arrange
            var config = Options.Create(new GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration
            {
                AccountEndpoint = "https://example.blob.core.windows.net/",
                ContainerName = "testcontainer",
                BlobPrefix = "",
                MaxConcurrentOperations = 4
            });

            var mockBlobClient = new Mock<BlobClient>(
                new Uri("https://example.blob.core.windows.net/testcontainer/models/testmodel/tables/dbo.tblA.json"),
                new Azure.Identity.DefaultAzureCredential(),
                new BlobClientOptions());

            // Simulate transient failures across uploads using a shared counter
            int uploadAttempts = 0;
            mockBlobClient.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    uploadAttempts++;
                    if (uploadAttempts <= 2)
                    {
                        throw new RequestFailedException(500, "Server error");
                    }

                    return Task.FromResult(Mock.Of<Response<BlobContentInfo>>());
                });

            var serializer = new Mock<ISecureJsonSerializer>();
            serializer.Setup(s => s.SerializeWithAuditAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<System.Text.Json.JsonSerializerOptions>()))
                .ReturnsAsync("{}\n");

            var strategy = new TestAzureBlobPersistenceStrategy(config, NullLogger<AzureBlobPersistenceStrategy>.Instance, serializer.Object)
            {
                BlobClientForTests = mockBlobClient.Object
            };

            // Inject a container client so EnsureContainerExistsAsync succeeds
            var mockContainer = new Mock<BlobContainerClient>(MockBehavior.Loose);
            mockContainer
                .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("test"), DateTimeOffset.UtcNow), Mock.Of<Response>()));
            var containerField = typeof(AzureBlobPersistenceStrategy).GetField("_containerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (containerField == null) throw new InvalidOperationException("_containerClient field not found");
            containerField.SetValue(strategy, mockContainer.Object);

            var model = new SemanticModel("testmodel", "source") { };
            model.Tables.Add(new SemanticModelTable("dbo", "tblA"));

            var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            tempDir.Create();

            try
            {
                // Act
                await strategy.SaveModelAsync(model, tempDir).ConfigureAwait(false);

                // Assert
                uploadAttempts.Should().BeGreaterThanOrEqualTo(3, "the upload should have retried at least twice before succeeding");
                mockBlobClient.Verify(b => b.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>()), Times.AtLeast(3), "UploadAsync should have been invoked repeatedly to handle transient failures");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir.FullName))
                    Directory.Delete(tempDir.FullName, true);
            }
        }

        [TestMethod]
        public async Task LoadEntityAsync_UnwrapsEnvelopeAndLoadsEntity()
        {
            // Arrange
            var config = Options.Create(new GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration
            {
                AccountEndpoint = "https://example.blob.core.windows.net/",
                ContainerName = "testcontainer",
                BlobPrefix = "",
                MaxConcurrentOperations = 4
            });

            var fakeBlobClient = new BlobClient(
                new Uri("https://example.blob.core.windows.net/testcontainer/models/testmodel/tables/dbo.tblA.json"),
                new Azure.Identity.DefaultAzureCredential(),
                new BlobClientOptions());

            // Prepare envelope JSON with 'data' property
            var envelope = "{ \"data\": { \"Schema\": \"dbo\", \"Name\": \"tblA\" }, \"embedding\": [1,2,3] }";

            // Setup delegate DownloadContentAsync to return the envelope
            var content = BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(envelope));

            var serializer = new Mock<ISecureJsonSerializer>();
            serializer.Setup(s => s.SerializeWithAuditAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<System.Text.Json.JsonSerializerOptions>()))
                .ReturnsAsync("{}\n");

            var strategy = new TestAzureBlobPersistenceStrategy(config, NullLogger<AzureBlobPersistenceStrategy>.Instance, serializer.Object)
            {
                BlobClientForTests = fakeBlobClient,
                OnDownloadContentAsync = (bc, ct) => Task.FromResult(Response.FromValue(content, Mock.Of<Response>()))
            };

            // Act
            string? receivedJson = null;
            await strategy.InvokeLoadEntityAsync("models/testmodel/tables/dbo.tblA.json", s => { receivedJson = s; return Task.CompletedTask; });

            // Assert
            receivedJson.Should().NotBeNullOrEmpty();
            receivedJson.Trim().Should().StartWith("{");
            receivedJson.Should().Contain("\"Schema\": \"dbo\"");
            receivedJson.Should().Contain("\"Name\": \"tblA\"");
        }

        [TestMethod]
        public async Task LoadEntityAsync_RetriesOnTransientDownloadFailure()
        {
            // Arrange
            var config = Options.Create(new GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration
            {
                AccountEndpoint = "https://example.blob.core.windows.net/",
                ContainerName = "testcontainer",
                BlobPrefix = "",
                MaxConcurrentOperations = 4
            });

            var fakeBlobClient = new BlobClient(
                new Uri("https://example.blob.core.windows.net/testcontainer/models/testmodel/tables/dbo.tblB.json"),
                new Azure.Identity.DefaultAzureCredential(),
                new BlobClientOptions());

            // Simulate transient failures across downloads using a shared counter
            int downloadAttempts = 0;
            Response<BlobDownloadResult> DownloadWithRetries(BlobClient _, CancellationToken __)
            {
                downloadAttempts++;
                if (downloadAttempts <= 2)
                {
                    throw new RequestFailedException(500, "Server error");
                }
                var envelope = "{ \"data\": { \"Schema\": \"dbo\", \"Name\": \"tblB\" }, \"embedding\": [4,5,6] }";
                var content = BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(envelope));
                return Response.FromValue(content, Mock.Of<Response>());
            }

            var serializer = new Mock<ISecureJsonSerializer>();
            serializer.Setup(s => s.SerializeWithAuditAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<System.Text.Json.JsonSerializerOptions>()))
                .ReturnsAsync("{}\n");

            var strategy = new TestAzureBlobPersistenceStrategy(config, NullLogger<AzureBlobPersistenceStrategy>.Instance, serializer.Object)
            {
                BlobClientForTests = fakeBlobClient,
                OnDownloadContentAsync = (bc, ct) => Task.FromResult(DownloadWithRetries(bc, ct))
            };

            // Act
            string? receivedJson = null;
            await strategy.InvokeLoadEntityAsync("models/testmodel/tables/dbo.tblB.json", s => { receivedJson = s; return Task.CompletedTask; });

            // Assert
            downloadAttempts.Should().BeGreaterThanOrEqualTo(3, "the download should have retried at least twice before succeeding");
            receivedJson.Should().NotBeNullOrEmpty();
            receivedJson.Should().Contain("\"Name\": \"tblB\"");
        }
    }

    [TestClass]
    public class AzureBlobPersistenceStrategyInitializationTests
    {
        [TestMethod]
        public void InitializeBlobServiceClient_UsesDefaultCredential_AndFactoryHooks()
        {
            // Arrange
            var config = Options.Create(new GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration
            {
                AccountEndpoint = "https://example.blob.core.windows.net/",
                ContainerName = "testcontainer",
                BlobPrefix = "",
                MaxConcurrentOperations = 4
            });

            var serializer = new Mock<ISecureJsonSerializer>();
            bool serviceCreated = false;
            bool containerCreated = false;

            var strategy = new TestAzureBlobPersistenceStrategy(config, NullLogger<AzureBlobPersistenceStrategy>.Instance, serializer.Object)
            {
                OnCreateDefaultCredential = () => new Azure.Identity.DefaultAzureCredential(),
                OnCreateBlobServiceClient = (endpoint, credential, options) => { serviceCreated = true; return new BlobServiceClient(endpoint, credential, options); },
                OnCreateBlobContainerClient = (serviceClient, containerName) => { containerCreated = true; return serviceClient.GetBlobContainerClient(containerName); }
            };

            // Act - invoke private InitializeBlobServiceClient via reflection
            var method = typeof(AzureBlobPersistenceStrategy).GetMethod("InitializeBlobServiceClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Should().NotBeNull();
            var result = method!.Invoke(strategy, Array.Empty<object>());
            result.Should().NotBeNull();

            // Assert hooks were invoked
            (serviceCreated && containerCreated).Should().BeTrue();
        }
    }

    // Minimal derived strategy for tests to override blob client retrieval and expose factory hooks
    internal class TestAzureBlobPersistenceStrategy : AzureBlobPersistenceStrategy
    {
        public TestAzureBlobPersistenceStrategy(IOptions<GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration> configuration, ILogger<AzureBlobPersistenceStrategy> logger, ISecureJsonSerializer secureJsonSerializer)
            : base(configuration, logger, secureJsonSerializer, null, skipInitialization: true)
        {
        }

        protected override BlobClient GetBlobClient(string blobName)
        {
            if (this.BlobClientForTests != null) return this.BlobClientForTests;
            return base.GetBlobClient(blobName);
        }

        // Factory hook overrides
        protected override TokenCredential CreateDefaultCredential()
            => OnCreateDefaultCredential?.Invoke() ?? base.CreateDefaultCredential();

        protected override BlobServiceClient CreateBlobServiceClient(Uri endpoint, TokenCredential credential, BlobClientOptions options)
            => OnCreateBlobServiceClient?.Invoke(endpoint, credential, options) ?? base.CreateBlobServiceClient(endpoint, credential, options);

        protected override BlobContainerClient CreateBlobContainerClient(BlobServiceClient serviceClient, string containerName)
            => OnCreateBlobContainerClient?.Invoke(serviceClient, containerName) ?? base.CreateBlobContainerClient(serviceClient, containerName);

        public Task InvokeLoadEntityAsync(string blobName, Func<string, Task> loader)
        {
            var method = typeof(AzureBlobPersistenceStrategy).GetMethod("LoadEntityAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null) throw new InvalidOperationException("LoadEntityAsync method not found");
            var task = (Task)method.Invoke(this, new object[] { blobName, loader })!;
            return task;
        }

        public BlobClient? BlobClientForTests { get; set; }
        public Func<BlobClient, CancellationToken, Task<Response<BlobDownloadResult>>>? OnDownloadContentAsync { get; set; }

        protected override Task<Response<BlobDownloadResult>> DownloadContentAsync(BlobClient blobClient, CancellationToken cancellationToken)
            => OnDownloadContentAsync?.Invoke(blobClient, cancellationToken) ?? blobClient.DownloadContentAsync(cancellationToken);

        public Func<TokenCredential>? OnCreateDefaultCredential { get; set; }
        public Func<Uri, TokenCredential, BlobClientOptions, BlobServiceClient>? OnCreateBlobServiceClient { get; set; }
        public Func<BlobServiceClient, string, BlobContainerClient>? OnCreateBlobContainerClient { get; set; }
    }

    // InitHookStrategy removed; we validate hooks via reflection invoking the initializer

    [TestClass]
    public class AzureBlobPersistenceStrategyAdditionalTests
    {
        [TestMethod]
        public async Task LoadEntityContentAsync_DownloadsOnlyRequestedBlob_AndUnwrapsEnvelope()
        {
            // Arrange
            var config = Options.Create(new GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration
            {
                AccountEndpoint = "https://example.blob.core.windows.net/",
                ContainerName = "testcontainer",
                BlobPrefix = "",
                MaxConcurrentOperations = 4
            });

            var serializer = new Mock<ISecureJsonSerializer>();
            var strategy = new TestAzureBlobPersistenceStrategyWithCounters(config, NullLogger<AzureBlobPersistenceStrategy>.Instance, serializer.Object);

            // Inject a container client that returns a BlobClient for any name
            var mockContainer = new Mock<BlobContainerClient>(MockBehavior.Loose);
            mockContainer.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns((string name) => new BlobClient(new Uri($"https://example.blob.core.windows.net/testcontainer/{name}"), new Azure.Identity.DefaultAzureCredential(), new BlobClientOptions()))
                ;

            var containerField = typeof(AzureBlobPersistenceStrategy).GetField("_containerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            containerField.Should().NotBeNull();
            containerField!.SetValue(strategy, mockContainer.Object);

            // Envelope content with top-level data
            strategy.OnDownloadContentAsync = (client, ct) =>
            {
                // Simulate that only the requested blob is ever downloaded
                var content = BlobsModelFactory.BlobDownloadResult(BinaryData.FromString("{ \"data\": { \"x\": 1 } }"));
                return Task.FromResult(Response.FromValue(content, Mock.Of<Response>()));
            };

            // Act
            var modelPath = new DirectoryInfo("testmodel");
            var json = await strategy.LoadEntityContentAsync(modelPath, "tables/dbo.tblA.json", CancellationToken.None);

            // Assert
            strategy.DownloadCount.Should().Be(1);
            strategy.LastDownloadedBlobName.Should().EndWith("models/testmodel/tables/dbo.tblA.json");
            json.Should().Be("{ \"x\": 1 }");
        }
        [TestMethod]
        public async Task DeleteBlobAsync_RetriesOnTransientFailure()
        {
            // Arrange
            var config = Options.Create(new GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration
            {
                AccountEndpoint = "https://example.blob.core.windows.net/",
                ContainerName = "testcontainer",
                BlobPrefix = "",
                MaxConcurrentOperations = 4
            });

            var mockBlobClient = new Mock<BlobClient>(MockBehavior.Loose);

            int deleteAttempts = 0;
            mockBlobClient.Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    deleteAttempts++;
                    if (deleteAttempts <= 2)
                        throw new RequestFailedException(500, "Server error");
                    return Task.FromResult(Response.FromValue(true, Mock.Of<Response>()));
                });

            var serializer = new Mock<ISecureJsonSerializer>();
            var strategy = new TestAzureBlobPersistenceStrategy(config, NullLogger<AzureBlobPersistenceStrategy>.Instance, serializer.Object)
            {
                BlobClientForTests = mockBlobClient.Object
            };

            // Act
            // Invoke private DeleteBlobAsync via reflection
            var method = typeof(AzureBlobPersistenceStrategy).GetMethod("DeleteBlobAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null) throw new InvalidOperationException("DeleteBlobAsync not found");
            await ((Task)method.Invoke(strategy, new object[] { mockBlobClient.Object, "models/m/test.json" })!);

            // Assert
            deleteAttempts.Should().BeGreaterThanOrEqualTo(3);
        }

        [TestMethod]
        public async Task ExistsAsync_ReturnsTrueWhenModelExists()
        {
            // Arrange
            var config = Options.Create(new GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration
            {
                AccountEndpoint = "https://example.blob.core.windows.net/",
                ContainerName = "testcontainer",
                BlobPrefix = "",
                MaxConcurrentOperations = 4
            });

            var mockContainer = new Mock<BlobContainerClient>(
                new Uri("https://example.blob.core.windows.net/testcontainer"),
                new Azure.Identity.DefaultAzureCredential(),
                new BlobClientOptions());
            var mockBlobClient = new Mock<BlobClient>(
                new Uri("https://example.blob.core.windows.net/testcontainer/models/testmodel/semanticmodel.json"),
                new Azure.Identity.DefaultAzureCredential(),
                new BlobClientOptions());

            mockBlobClient.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
            mockContainer.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            var serializer = new Mock<ISecureJsonSerializer>();
            var strategy = new TestAzureBlobPersistenceStrategy(config, NullLogger<AzureBlobPersistenceStrategy>.Instance, serializer.Object)
            {
                BlobClientForTests = mockBlobClient.Object
            };

            // inject container so ExistsAsync can resolve the blob client
            var containerField2 = typeof(AzureBlobPersistenceStrategy).GetField("_containerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (containerField2 == null) throw new InvalidOperationException("_containerClient field not found");
            containerField2.SetValue(strategy, mockContainer.Object);

            // Act
            var exists = await strategy.ExistsAsync(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "testmodel")));

            // Assert
            exists.Should().BeTrue();
        }

        [TestMethod]
        public async Task ListModelsAsync_ParsesModelNames()
        {
            // Arrange
            var config = Options.Create(new GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration
            {
                AccountEndpoint = "https://example.blob.core.windows.net/",
                ContainerName = "testcontainer",
                BlobPrefix = "",
                MaxConcurrentOperations = 4
            });

            var mockContainer = new Mock<BlobContainerClient>(
                new Uri("https://example.blob.core.windows.net/testcontainer"),
                new Azure.Identity.DefaultAzureCredential(),
                new BlobClientOptions());

            var blobs = new[] {
                BlobsModelFactory.BlobItem(name: "models/modelA/semanticmodel.json"),
                BlobsModelFactory.BlobItem(name: "models/modelA/tables/a.json"),
                BlobsModelFactory.BlobItem(name: "models/modelB/semanticmodel.json"),
                BlobsModelFactory.BlobItem(name: "models/modelC/index.json")
            };

            mockContainer.Setup(c => c.GetBlobsAsync(It.IsAny<Azure.Storage.Blobs.Models.BlobTraits>(), It.IsAny<Azure.Storage.Blobs.Models.BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(CreateAsyncPageable(blobs));

            var serializer = new Mock<ISecureJsonSerializer>();
            var strategy = new TestAzureBlobPersistenceStrategy(config, NullLogger<AzureBlobPersistenceStrategy>.Instance, serializer.Object)
            {
                BlobClientForTests = null
            };

            var containerField = typeof(AzureBlobPersistenceStrategy).GetField("_containerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (containerField == null) throw new InvalidOperationException("_containerClient field not found");
            containerField.SetValue(strategy, mockContainer.Object);

            // Act
            var models = await strategy.ListModelsAsync(new DirectoryInfo(Path.GetTempPath()));

            // Assert
            models.Should().BeEquivalentTo(new[] { "modelA", "modelB", "modelC" });
        }

        // Helper moved to class scope to avoid local-class declaration issues
        private static Azure.AsyncPageable<BlobItem> CreateAsyncPageable(BlobItem[] items)
        {
            return new TestAsyncPageable(items);
        }

        private class TestAsyncPageable : Azure.AsyncPageable<BlobItem>
        {
            private readonly IReadOnlyList<BlobItem> _items;

            public TestAsyncPageable(IEnumerable<BlobItem> items)
            {
                _items = items.ToList();
            }

            public override async IAsyncEnumerable<Azure.Page<BlobItem>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
            {
                yield return Azure.Page<BlobItem>.FromValues(_items, null, Mock.Of<Response>());
                await Task.CompletedTask;
            }
        }
    }

    internal class TestAzureBlobPersistenceStrategyWithCounters : TestAzureBlobPersistenceStrategy
    {
        public int DownloadCount { get; private set; }
        public string? LastDownloadedBlobName { get; private set; }

        public TestAzureBlobPersistenceStrategyWithCounters(IOptions<GenAIDBExplorer.Core.Models.Project.AzureBlobStorageConfiguration> configuration, ILogger<AzureBlobPersistenceStrategy> logger, ISecureJsonSerializer secureJsonSerializer)
            : base(configuration, logger, secureJsonSerializer)
        {
        }

        public new Func<BlobClient, CancellationToken, Task<Response<BlobDownloadResult>>>? OnDownloadContentAsync { get; set; }

        protected override Task<Response<BlobDownloadResult>> DownloadContentAsync(BlobClient blobClient, CancellationToken cancellationToken)
        {
            DownloadCount++;
            LastDownloadedBlobName = blobClient.Uri.AbsolutePath.TrimStart('/');
            if (OnDownloadContentAsync != null)
            {
                return OnDownloadContentAsync(blobClient, cancellationToken);
            }
            return base.DownloadContentAsync(blobClient, cancellationToken);
        }
    }
}
