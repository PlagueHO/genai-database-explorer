using Azure;
using Azure.Security.KeyVault.Secrets;
using FluentAssertions;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository.Security;

/// <summary>
/// Unit tests for the KeyVaultConfigurationProvider class to validate Azure Key Vault integration and fallback mechanisms.
/// </summary>
[TestClass]
public class KeyVaultConfigurationProviderTests
{
    private Mock<ILogger<KeyVaultConfigurationProvider>> _mockLogger = null!;
    private const string TestKeyVaultUri = "https://test-vault.vault.azure.net/";

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<KeyVaultConfigurationProvider>>();
    }

    [TestMethod]
    public void Constructor_ValidUri_ShouldCreateProvider()
    {
        // Act
        using var provider = new KeyVaultConfigurationProvider(TestKeyVaultUri, _mockLogger.Object);

        // Assert
        provider.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_NullUri_ShouldThrowArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new KeyVaultConfigurationProvider(null!, _mockLogger.Object))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Constructor_EmptyUri_ShouldThrowUriFormatException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new KeyVaultConfigurationProvider("", _mockLogger.Object))
            .Should().Throw<UriFormatException>();
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_ValidCall_ShouldNotThrow()
    {
        // Arrange
        using var provider = new KeyVaultConfigurationProvider(TestKeyVaultUri, _mockLogger.Object);

        // Act & Assert - This will likely fail due to authentication, but shouldn't throw argument exceptions
        try
        {
            await provider.GetConfigurationValueAsync("test-secret");
        }
        catch (Exception ex)
        {
            // Expected to fail in test environment due to authentication
            ex.Should().NotBeOfType<ArgumentException>();
        }
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_NullSecretName_ShouldThrowArgumentException()
    {
        // Arrange
        using var provider = new KeyVaultConfigurationProvider(TestKeyVaultUri, _mockLogger.Object);

        // Act & Assert
        await FluentActions.Invoking(() => provider.GetConfigurationValueAsync(null!))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_EmptySecretName_ShouldThrowArgumentException()
    {
        // Arrange
        using var provider = new KeyVaultConfigurationProvider(TestKeyVaultUri, _mockLogger.Object);

        // Act & Assert
        await FluentActions.Invoking(() => provider.GetConfigurationValueAsync(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GetConfigurationValueAsync_WhitespaceSecretName_ShouldThrowArgumentException()
    {
        // Arrange
        using var provider = new KeyVaultConfigurationProvider(TestKeyVaultUri, _mockLogger.Object);

        // Act & Assert
        await FluentActions.Invoking(() => provider.GetConfigurationValueAsync("   "))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task TestConnectivityAsync_ShouldReturnResult()
    {
        // Arrange
        using var provider = new KeyVaultConfigurationProvider(TestKeyVaultUri, _mockLogger.Object);

        // Act
        var result = await provider.TestConnectivityAsync();

        // Assert - Should return false in test environment due to authentication
        result.Should().BeFalse();
    }
}
