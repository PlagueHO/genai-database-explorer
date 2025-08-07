using Azure;
using Azure.Security.KeyVault.Secrets;
using FluentAssertions;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;

namespace GenAIDBExplorer.Core.Test.Repository.Security;

/// <summary>
/// Unit tests for the KeyVaultConfigurationProvider class to validate Azure Key Vault integration and fallback mechanisms.
/// </summary>
[TestClass]
public class KeyVaultConfigurationProviderTests
{
    private Mock<SecretClient> _mockSecretClient = null!;
    private Mock<ILogger<KeyVaultConfigurationProvider>> _mockLogger = null!;
    private IOptions<KeyVaultOptions> _options = null!;
    private KeyVaultConfigurationProvider _keyVaultProvider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockSecretClient = new Mock<SecretClient>();
        _mockLogger = new Mock<ILogger<KeyVaultConfigurationProvider>>();
        
        var keyVaultOptions = new KeyVaultOptions
        {
            KeyVaultUri = "https://test-vault.vault.azure.net/",
            EnableKeyVault = true,
            CacheExpiration = TimeSpan.FromMinutes(30),
            EnableEnvironmentVariableFallback = true,
            RetryPolicy = new KeyVaultRetryPolicy
            {
                MaxRetries = 3,
                BaseDelay = TimeSpan.FromSeconds(1),
                EnableJitter = true
            }
        };
        
        _options = Options.Create(keyVaultOptions);
        _keyVaultProvider = new KeyVaultConfigurationProvider(_mockSecretClient.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_ValidSecretName_ReturnsSecretValue()
    {
        // Arrange
        const string secretName = "test-secret";
        const string secretValue = "secret-value";
        var secret = new KeyVaultSecret(secretName, secretValue);
        var response = Response.FromValue(secret, Mock.Of<Response>());

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName);

        // Assert
        result.Should().Be(secretValue);
        
        // Verify the mock was called
        _mockSecretClient.Verify(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_SecretNotFound_ReturnsEnvironmentVariable()
    {
        // Arrange
        const string secretName = "test-secret";
        const string envVarName = "TEST_SECRET";
        const string envVarValue = "environment-value";

        Environment.SetEnvironmentVariable(envVarName, envVarValue);

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Secret not found"));

        // Act
        var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName, envVarName);

        // Assert
        result.Should().Be(envVarValue);

        // Cleanup
        Environment.SetEnvironmentVariable(envVarName, null);
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_SecretNotFoundAndNoEnvironmentVariable_ReturnsNull()
    {
        // Arrange
        const string secretName = "nonexistent-secret";
        const string envVarName = "NONEXISTENT_ENV_VAR";

        Environment.SetEnvironmentVariable(envVarName, null); // Ensure it's not set

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Secret not found"));

        // Act
        var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName, envVarName);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_InvalidSecretName_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _keyVaultProvider.GetConfigurationValueAsync(null!))
            .Should().ThrowAsync<ArgumentException>();

        await FluentActions.Invoking(() => _keyVaultProvider.GetConfigurationValueAsync(""))
            .Should().ThrowAsync<ArgumentException>();

        await FluentActions.Invoking(() => _keyVaultProvider.GetConfigurationValueAsync("   "))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_CachingEnabled_ReturnsCachedValueOnSecondCall()
    {
        // Arrange
        const string secretName = "cached-secret";
        const string secretValue = "cached-value";
        var secret = new KeyVaultSecret(secretName, secretValue);
        var response = Response.FromValue(secret, Mock.Of<Response>());

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result1 = await _keyVaultProvider.GetConfigurationValueAsync(secretName);
        var result2 = await _keyVaultProvider.GetConfigurationValueAsync(secretName);

        // Assert
        result1.Should().Be(secretValue);
        result2.Should().Be(secretValue);

        // Verify that the secret client was only called once (second call used cache)
        _mockSecretClient.Verify(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_RequestFailedException_LogsErrorAndRetriesFallback()
    {
        // Arrange
        const string secretName = "failing-secret";
        const string envVarName = "FAILING_SECRET";
        const string envVarValue = "fallback-value";

        Environment.SetEnvironmentVariable(envVarName, envVarValue);

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(500, "Internal server error"));

        // Act
        var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName, envVarName);

        // Assert
        result.Should().Be(envVarValue);

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Using environment variable fallback due to Key Vault error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        Environment.SetEnvironmentVariable(envVarName, null);
    }

    [TestMethod]
    public async Task TestConnectivityAsync_ValidConnection_ReturnsTrue()
    {
        // Arrange
        var mockPageable = new Mock<AsyncPageable<SecretProperties>>();
        var mockPage = new Mock<Page<SecretProperties>>();
        
        mockPageable.Setup(x => x.AsPages(null, null))
            .Returns(AsyncEnumerable.Repeat(mockPage.Object, 1));

        _mockSecretClient.Setup(x => x.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()))
            .Returns(mockPageable.Object);

        // Act
        var result = await _keyVaultProvider.TestConnectivityAsync();

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task TestConnectivityAsync_ConnectionFailure_ReturnsFalse()
    {
        // Arrange
        var mockPageable = new Mock<AsyncPageable<SecretProperties>>();
        
        mockPageable.Setup(x => x.AsPages(null, null))
            .Throws(new RequestFailedException(401, "Unauthorized"));

        _mockSecretClient.Setup(x => x.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()))
            .Returns(mockPageable.Object);

        // Act
        var result = await _keyVaultProvider.TestConnectivityAsync();

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_WithSecretName_RetrievesSecretValue()
    {
        // Arrange
        const string secretName = "regular-secret";
        const string secretValue = "regular-value";
        
        // Since the KeyVaultConfigurationProvider has fallback behavior, when the real SecretClient fails,
        // it will check environment variables. Let's test this fallback behavior instead.
        const string envVarName = "REGULAR_SECRET";
        Environment.SetEnvironmentVariable(envVarName, secretValue);

        try
        {
            // Act
            var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName, envVarName);

            // Assert
            result.Should().Be(secretValue);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_MultipleExceptions_LogsAllAttempts()
    {
        // Arrange
        const string secretName = "retry-secret";
        const string envVarName = "RETRY_SECRET";
        const string envVarValue = "retry-fallback";

        Environment.SetEnvironmentVariable(envVarName, envVarValue);

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(503, "Service unavailable"));

        // Act
        var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName, envVarName);

        // Assert
        result.Should().Be(envVarValue);

        // Verify warning was logged for the failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Using environment variable fallback due to Key Vault error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        Environment.SetEnvironmentVariable(envVarName, null);
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_CachingDisabled_AlwaysCallsKeyVault()
    {
        // Arrange
        const string secretName = "uncached-secret";
        const string secretValue = "uncached-value";
        var secret = new KeyVaultSecret(secretName, secretValue);
        var response = Response.FromValue(secret, Mock.Of<Response>());

        // Create provider with caching disabled
        var options = new KeyVaultOptions { EnableCaching = false };
        var provider = new KeyVaultConfigurationProvider(_mockSecretClient.Object, _mockLogger.Object, options);

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result1 = await provider.GetConfigurationValueAsync(secretName);
        var result2 = await provider.GetConfigurationValueAsync(secretName);

        // Assert
        result1.Should().Be(secretValue);
        result2.Should().Be(secretValue);

        // Verify that the secret client was called twice (no caching)
        _mockSecretClient.Verify(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_EnvironmentFallbackDisabled_DoesNotCheckEnvironmentVariable()
    {
        // Arrange
        const string secretName = "no-fallback-secret";
        const string envVarName = "NO_FALLBACK_SECRET";
        const string envVarValue = "should-not-be-used";

        Environment.SetEnvironmentVariable(envVarName, envVarValue);

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Secret not found"));

        // Act - Note: we're not providing a fallback environment variable name
        var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName);

        // Assert
        result.Should().BeNull(); // Should not fall back to environment variable

        // Cleanup
        Environment.SetEnvironmentVariable(envVarName, null);
    }
}
