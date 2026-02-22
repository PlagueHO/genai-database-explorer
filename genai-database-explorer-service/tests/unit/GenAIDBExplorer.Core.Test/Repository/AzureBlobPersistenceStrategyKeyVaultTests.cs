using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository
{
    [TestClass]
    public class AzureBlobPersistenceStrategyKeyVaultTests
    {
        private sealed class TestKeyVaultProvider : KeyVaultConfigurationProvider
        {
            public TestKeyVaultProvider()
                : base("https://vault/", NullLogger<KeyVaultConfigurationProvider>.Instance)
            {
            }

            public override Task<string?> GetConfigurationValueAsync(string keyName, string? fallbackEnvironmentVariable = null, string? defaultValue = null)
            {
                return Task.FromResult<string?>("DefaultEndpointsProtocol=https;AccountName=fake;AccountKey=fake;EndpointSuffix=core.windows.net");
            }
        }

        private sealed class TestStrategyWithKv : AzureBlobPersistenceStrategy
        {
            public TestStrategyWithKv(AzureBlobConfiguration cfg, ILogger<AzureBlobPersistenceStrategy> logger, ISecureJsonSerializer serializer, KeyVaultConfigurationProvider kv)
                : base(cfg, logger, serializer, kv, skipInitialization: true)
            {
            }

            public Func<TokenCredential>? OnCreateDefaultCredential { get; set; }
            public Func<Uri, TokenCredential, BlobClientOptions, BlobServiceClient>? OnCreateServiceFromUri { get; set; }
            public Func<string, BlobClientOptions, BlobServiceClient>? OnCreateServiceFromConn { get; set; }
            public Func<BlobServiceClient, string, BlobContainerClient>? OnCreateContainer { get; set; }
            public Func<BlobClient, CancellationToken, Task<Response<BlobDownloadResult>>>? OnDownload { get; set; }
            public Func<string, BlobClient>? OnGetBlobClient { get; set; }

            protected override TokenCredential CreateDefaultCredential() => OnCreateDefaultCredential?.Invoke() ?? new DefaultAzureCredential();
            protected override BlobServiceClient CreateBlobServiceClient(Uri endpoint, TokenCredential credential, BlobClientOptions options)
                => OnCreateServiceFromUri?.Invoke(endpoint, credential, options) ?? new BlobServiceClient(endpoint, credential, options);
            protected override BlobServiceClient CreateBlobServiceClient(string connectionString, BlobClientOptions options)
                => OnCreateServiceFromConn?.Invoke(connectionString, options) ?? new BlobServiceClient(connectionString, options);
            protected override BlobContainerClient CreateBlobContainerClient(BlobServiceClient serviceClient, string containerName)
                => OnCreateContainer?.Invoke(serviceClient, containerName) ?? serviceClient.GetBlobContainerClient(containerName);
            protected override BlobClient GetBlobClient(string blobName)
                => OnGetBlobClient?.Invoke(blobName) ?? base.GetBlobClient(blobName);
            protected override Task<Response<BlobDownloadResult>> DownloadContentAsync(BlobClient blobClient, CancellationToken cancellationToken)
                => OnDownload?.Invoke(blobClient, cancellationToken) ?? blobClient.DownloadContentAsync(cancellationToken);
        }

        [TestMethod]
        public async Task LoadModelAsync_RefreshesClients_WhenKeyVaultProvidesConnectionString()
        {
            // Arrange
            var cfg = Options.Create(new AzureBlobConfiguration
            {
                AccountEndpoint = "https://example.blob.core.windows.net/",
                ContainerName = "testcontainer",
                BlobPrefix = "models",
                MaxConcurrentOperations = 2
            });

            // Key Vault provider returns a connection string (test subclass)
            var kvProvider = new TestKeyVaultProvider();

            var serializer = new Mock<ISecureJsonSerializer>();
            serializer.Setup(s => s.SerializeWithAuditAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<System.Text.Json.JsonSerializerOptions>()))
                      .ReturnsAsync("{}\n");
            serializer.Setup(s => s.DeserializeAsync<SemanticModel>(It.IsAny<string>(), It.IsAny<System.Text.Json.JsonSerializerOptions>()))
                      .ReturnsAsync(new SemanticModel("test", "src"));

            var createdFromConn = 0;
            var createdFromUri = 0;

            var containerMock = new Mock<BlobContainerClient>(MockBehavior.Loose);
            var mainBlobMock = new Mock<BlobClient>(MockBehavior.Loose);

            // semanticmodel.json existence
            mainBlobMock.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            // download returns a minimal model json
            var modelJson = "{\"name\":\"test\",\"source\":\"src\",\"tables\":[],\"views\":[],\"storedProcedures\":[]}";
            var downloadResult = BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(modelJson));

            var serviceFromConn = new Mock<BlobServiceClient>(MockBehavior.Loose);
            serviceFromConn.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(containerMock.Object);

            // Hook container client to return our blob for semantic model
            containerMock.Setup(c => c.GetBlobClient(It.Is<string>(n => n.EndsWith("semanticmodel.json"))))
                         .Returns(mainBlobMock.Object);

            var strategy = new TestStrategyWithKv(cfg.Value, NullLogger<AzureBlobPersistenceStrategy>.Instance, serializer.Object, kvProvider)
            {
                OnCreateServiceFromConn = (cs, opts) => { createdFromConn++; return serviceFromConn.Object; },
                OnCreateServiceFromUri = (u, cred, opts) => { createdFromUri++; return new BlobServiceClient(u, cred, opts); },
                OnCreateContainer = (svc, name) => containerMock.Object,
                OnGetBlobClient = name => name.EndsWith("semanticmodel.json") ? mainBlobMock.Object : new BlobClient(new Uri(cfg.Value.AccountEndpoint + name), new DefaultAzureCredential()),
                OnDownload = (bc, ct) => Task.FromResult(Response.FromValue(downloadResult, Mock.Of<Response>()))
            };

            // Act
            var model = await strategy.LoadModelAsync(new DirectoryInfo("testmodel")).ConfigureAwait(false);

            // Assert
            createdFromConn.Should().BeGreaterThanOrEqualTo(1, "Key Vault connection string should trigger client refresh via connection string path");
            model.Should().NotBeNull();
            model.Name.Should().Be("test");
        }
    }
}
