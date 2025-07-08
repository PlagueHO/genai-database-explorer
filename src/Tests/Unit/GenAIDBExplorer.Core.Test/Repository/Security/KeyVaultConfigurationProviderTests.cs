using Azure;
using Azure.Security.KeyVault.Secrets;
using FluentAssertions;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
            VaultUri = "https://test-vault.vault.azure.net/",
            EnableCaching = true,
            CacheExpirationMinutes = 30,
            EnableEnvironmentVariableFallback = true,
            RetryPolicy = new KeyVaultRetryPolicy
            {
                MaxRetries = 3,
                DelayBetweenRetriesMs = 1000,
                ExponentialBackoff = true
            }
        };
        
        _options = Options.Create(keyVaultOptions);
        _keyVaultProvider = new KeyVaultConfigurationProvider(_options, _mockLogger.Object, _mockSecretClient.Object);
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_ValidSecretName_ReturnsSecretValue()
    {
        // Arrange
        const string secretName = "test-secret";
        const string secretValue = "secret-value";
        var secret = new KeyVaultSecret(secretName, secretValue);
        var response = Response.FromValue(secret, Mock.Of<Response>());

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, default))
            .ReturnsAsync(response);

        // Act
        var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName);

        // Assert
        result.Should().Be(secretValue);
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_SecretNotFound_ReturnsEnvironmentVariable()
    {
        // Arrange
        const string secretName = "test-secret";
        const string envVarName = "TEST_SECRET";
        const string envVarValue = "environment-value";

        Environment.SetEnvironmentVariable(envVarName, envVarValue);

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, default))
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

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, default))
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

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, default))
            .ReturnsAsync(response);

        // Act
        var result1 = await _keyVaultProvider.GetConfigurationValueAsync(secretName);
        var result2 = await _keyVaultProvider.GetConfigurationValueAsync(secretName);

        // Assert
        result1.Should().Be(secretValue);
        result2.Should().Be(secretValue);

        // Verify that the secret client was only called once (second call used cache)
        _mockSecretClient.Verify(x => x.GetSecretAsync(secretName, null, default), Times.Once);
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_RequestFailedException_LogsErrorAndRetriesFallback()
    {
        // Arrange
        const string secretName = "failing-secret";
        const string envVarName = "FAILING_SECRET";
        const string envVarValue = "fallback-value";

        Environment.SetEnvironmentVariable(envVarName, envVarValue);

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, default))
            .ThrowsAsync(new RequestFailedException(500, "Internal server error"));

        // Act
        var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName, envVarName);

        // Assert
        result.Should().Be(envVarValue);

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to retrieve secret from Key Vault")),
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
        var secret = new KeyVaultSecret("connectivity-test", "test-value");
        var response = Response.FromValue(secret, Mock.Of<Response>());

        _mockSecretClient.Setup(x => x.GetSecretAsync("connectivity-test", null, default))
            .ReturnsAsync(response);

        // Act
        var result = await _keyVaultProvider.TestConnectivityAsync();

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task TestConnectivityAsync_ConnectionFailure_ReturnsFalse()
    {
        // Arrange
        _mockSecretClient.Setup(x => x.GetSecretAsync("connectivity-test", null, default))
            .ThrowsAsync(new RequestFailedException(401, "Unauthorized"));

        // Act
        var result = await _keyVaultProvider.TestConnectivityAsync();

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_WithVersion_RetrievesSpecificVersion()
    {
        // Arrange
        const string secretName = "versioned-secret";
        const string version = "v1.0";
        const string secretValue = "versioned-value";
        var secret = new KeyVaultSecret(secretName, secretValue);
        var response = Response.FromValue(secret, Mock.Of<Response>());

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, version, default))
            .ReturnsAsync(response);

        // Act
        var result = await _keyVaultProvider.GetConfigurationValueAsync(secretName, version: version);

        // Assert
        result.Should().Be(secretValue);
        _mockSecretClient.Verify(x => x.GetSecretAsync(secretName, version, default), Times.Once);
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_MultipleExceptions_LogsAllAttempts()
    {
        // Arrange
        const string secretName = "retry-secret";
        const string envVarName = "RETRY_SECRET";
        const string envVarValue = "retry-fallback";

        Environment.SetEnvironmentVariable(envVarName, envVarValue);

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, default))
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to retrieve secret from Key Vault")),
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
        var keyVaultOptionsWithoutCache = new KeyVaultOptions
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            EnableCaching = false,
            EnableEnvironmentVariableFallback = true
        };
        
        var optionsWithoutCache = Options.Create(keyVaultOptionsWithoutCache);
        var providerWithoutCache = new KeyVaultConfigurationProvider(optionsWithoutCache, _mockLogger.Object, _mockSecretClient.Object);

        const string secretName = "uncached-secret";
        const string secretValue = "uncached-value";
        var secret = new KeyVaultSecret(secretName, secretValue);
        var response = Response.FromValue(secret, Mock.Of<Response>());

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, default))
            .ReturnsAsync(response);

        // Act
        var result1 = await providerWithoutCache.GetConfigurationValueAsync(secretName);
        var result2 = await providerWithoutCache.GetConfigurationValueAsync(secretName);

        // Assert
        result1.Should().Be(secretValue);
        result2.Should().Be(secretValue);

        // Verify that the secret client was called twice (no caching)
        _mockSecretClient.Verify(x => x.GetSecretAsync(secretName, null, default), Times.Exactly(2));
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_EnvironmentFallbackDisabled_DoesNotCheckEnvironmentVariable()
    {
        // Arrange
        var keyVaultOptionsWithoutFallback = new KeyVaultOptions
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            EnableCaching = true,
            EnableEnvironmentVariableFallback = false
        };
        
        var optionsWithoutFallback = Options.Create(keyVaultOptionsWithoutFallback);
        var providerWithoutFallback = new KeyVaultConfigurationProvider(optionsWithoutFallback, _mockLogger.Object, _mockSecretClient.Object);

        const string secretName = "no-fallback-secret";
        const string envVarName = "NO_FALLBACK_SECRET";
        const string envVarValue = "should-not-be-used";

        Environment.SetEnvironmentVariable(envVarName, envVarValue);

        _mockSecretClient.Setup(x => x.GetSecretAsync(secretName, null, default))
            .ThrowsAsync(new RequestFailedException(404, "Secret not found"));

        // Act
        var result = await providerWithoutFallback.GetConfigurationValueAsync(secretName, envVarName);

        // Assert
        result.Should().BeNull(); // Should not fall back to environment variable

        // Cleanup
        Environment.SetEnvironmentVariable(envVarName, null);
    }
}
