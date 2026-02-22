using FluentAssertions;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.Repository.Security;

/// <summary>
/// Integration tests for Phase 5b security features to validate end-to-end functionality.
/// </summary>
[TestClass]
public class SecurityIntegrationTests
{
    private IHost _host = null!;
    private IServiceScope _scope = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        // Create a minimal host for dependency injection testing
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Add the security services using the same configuration as the main application
                services.Configure<SecureJsonSerializerOptions>(options =>
                {
                    options.MaxJsonDepth = 64;
                    options.MaxStringLength = 50 * 1024 * 1024; // 50MB
                    options.EnableAuditLogging = true;
                });

                services.Configure<KeyVaultOptions>(options =>
                {
                    options.KeyVaultUri = "https://test-vault.vault.azure.net/";
                    options.EnableKeyVault = true;
                    options.CacheExpiration = TimeSpan.FromMinutes(30);
                    options.EnableEnvironmentVariableFallback = true;
                });

                // Register the secure JSON serializer
                services.AddSingleton<ISecureJsonSerializer, SecureJsonSerializer>();

                // Register Key Vault provider conditionally (only if vault URI is configured)
                services.AddSingleton<KeyVaultConfigurationProvider>(serviceProvider =>
                {
                    var keyVaultOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeyVaultOptions>>();
                    var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KeyVaultConfigurationProvider>>();

                    if (!string.IsNullOrEmpty(keyVaultOptions.Value.KeyVaultUri))
                    {
                        return new KeyVaultConfigurationProvider("https://test-vault.vault.azure.net/", logger);
                    }

                    return null!; // Will be null if not configured
                });
            })
            .Build();

        _scope = _host.Services.CreateScope();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope?.Dispose();
        _host?.Dispose();
    }

    [TestMethod]
    public void DependencyInjection_SecureJsonSerializer_CanBeResolved()
    {
        // Act
        var secureJsonSerializer = _scope.ServiceProvider.GetService<ISecureJsonSerializer>();

        // Assert
        secureJsonSerializer.Should().NotBeNull();
        secureJsonSerializer.Should().BeOfType<SecureJsonSerializer>();
    }

    [TestMethod]
    public void DependencyInjection_KeyVaultProvider_CanBeResolved()
    {
        // Act
        var keyVaultProvider = _scope.ServiceProvider.GetService<KeyVaultConfigurationProvider>();

        // Assert
        keyVaultProvider.Should().NotBeNull();
        keyVaultProvider.Should().BeOfType<KeyVaultConfigurationProvider>();
    }

    [TestMethod]
    public async Task SecureJsonSerializer_EndToEndSerialization_WorksCorrectly()
    {
        // Arrange
        var secureJsonSerializer = _scope.ServiceProvider.GetRequiredService<ISecureJsonSerializer>();
        var testObject = new SecurityTestObject
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            Description = "This is a test object for security validation",
            CreatedAt = DateTime.UtcNow,
            Tags = new[] { "test", "security", "validation" },
            Metadata = new Dictionary<string, object>
            {
                { "version", "1.0" },
                { "secure", true },
                { "level", 42 }
            }
        };

        // Act - Serialize
        var json = await secureJsonSerializer.SerializeAsync(testObject);

        // Act - Validate security
        var isSecure = await secureJsonSerializer.ValidateJsonSecurityAsync(json);

        // Act - Deserialize
        var deserializedObject = await secureJsonSerializer.DeserializeAsync<SecurityTestObject>(json);

        // Assert
        json.Should().NotBeNullOrEmpty();
        isSecure.Should().BeTrue();
        deserializedObject.Should().NotBeNull();
        deserializedObject!.Id.Should().Be(testObject.Id);
        deserializedObject.Name.Should().Be(testObject.Name);
        deserializedObject.Description.Should().Be(testObject.Description);
        deserializedObject.Tags.Should().BeEquivalentTo(testObject.Tags);
    }

    [TestMethod]
    public async Task SecureJsonSerializer_SecurityValidation_DetectsDangerousContent()
    {
        // Arrange
        var secureJsonSerializer = _scope.ServiceProvider.GetRequiredService<ISecureJsonSerializer>();
        var dangerousJson = "{\"content\":\"<script>alert('xss')</script>\",\"url\":\"javascript:void(0)\"}";

        // Act
        var isSecure = await secureJsonSerializer.ValidateJsonSecurityAsync(dangerousJson);

        // Assert
        isSecure.Should().BeFalse();
    }

    [TestMethod]
    public async Task SecureJsonSerializer_Sanitization_RemovesDangerousPatterns()
    {
        // Arrange
        var secureJsonSerializer = _scope.ServiceProvider.GetRequiredService<ISecureJsonSerializer>();
        var dangerousJson = "{\"description\":\"Visit <script>alert('hack')</script> for more info\"}";

        // Act
        var sanitizedJson = await secureJsonSerializer.SanitizeJsonAsync(dangerousJson);

        // Assert
        sanitizedJson.Should().NotContain("<script>");
        sanitizedJson.Should().Contain("[SANITIZED]");
    }

    [TestMethod]
    public async Task SecureJsonSerializer_AuditedSerialization_CompletesSuccessfully()
    {
        // Arrange
        var secureJsonSerializer = _scope.ServiceProvider.GetRequiredService<ISecureJsonSerializer>();
        var testObject = new { Message = "This is an audited operation", Timestamp = DateTime.UtcNow };
        var operationContext = "SecurityIntegrationTest";

        // Act
        var json = await secureJsonSerializer.SerializeWithAuditAsync(testObject, operationContext);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"message\":");
        json.Should().Contain("\"timestamp\":");
    }

    [TestMethod]
    public void SecurityConfiguration_OptionsBinding_WorksCorrectly()
    {
        // Arrange & Act
        var secureJsonOptions = _scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureJsonSerializerOptions>>();
        var keyVaultOptions = _scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeyVaultOptions>>();

        // Assert
        secureJsonOptions.Should().NotBeNull();
        secureJsonOptions.Value.MaxJsonDepth.Should().Be(64);
        secureJsonOptions.Value.MaxStringLength.Should().Be(50 * 1024 * 1024);
        secureJsonOptions.Value.EnableAuditLogging.Should().BeTrue();

        keyVaultOptions.Should().NotBeNull();
        keyVaultOptions.Value.KeyVaultUri.Should().Be("https://test-vault.vault.azure.net/");
        keyVaultOptions.Value.EnableKeyVault.Should().BeTrue();
        keyVaultOptions.Value.CacheExpiration.Should().Be(TimeSpan.FromMinutes(30));
        keyVaultOptions.Value.EnableEnvironmentVariableFallback.Should().BeTrue();
    }
}

/// <summary>
/// Test object for comprehensive security testing.
/// </summary>
public class SecurityTestObject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
