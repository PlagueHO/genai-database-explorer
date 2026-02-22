using System.Text.Json;
using FluentAssertions;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Repository.Security;

/// <summary>
/// Unit tests for the SecureJsonSerializer class to validate security features and JSON processing.
/// </summary>
[TestClass]
public class SecureJsonSerializerTests
{
    private Mock<ILogger<SecureJsonSerializer>> _mockLogger = null!;
    private SecureJsonSerializer _secureJsonSerializer = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<SecureJsonSerializer>>();
        _secureJsonSerializer = new SecureJsonSerializer(_mockLogger.Object);
    }

    [TestMethod]
    public async Task SerializeAsync_ValidObject_ReturnsJsonString()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 42 };

        // Act
        var result = await _secureJsonSerializer.SerializeAsync(testObject);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"name\":");
        result.Should().Contain("\"value\":");
    }

    [TestMethod]
    public async Task SerializeAsync_NullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _secureJsonSerializer.SerializeAsync<object>(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task DeserializeAsync_ValidJson_ReturnsObject()
    {
        // Arrange
        var json = "{\"name\":\"Test\",\"value\":42}";

        // Act
        var result = await _secureJsonSerializer.DeserializeAsync<TestObject>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [TestMethod]
    public async Task DeserializeAsync_NullOrEmptyJson_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _secureJsonSerializer.DeserializeAsync<TestObject>(null!))
            .Should().ThrowAsync<ArgumentException>();

        await FluentActions.Invoking(() => _secureJsonSerializer.DeserializeAsync<TestObject>(""))
            .Should().ThrowAsync<ArgumentException>();

        await FluentActions.Invoking(() => _secureJsonSerializer.DeserializeAsync<TestObject>("   "))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task DeserializeAsync_MalformedJson_ThrowsArgumentException()
    {
        // Arrange
        var malformedJson = "{invalid json}";

        // Act & Assert
        await FluentActions.Invoking(() => _secureJsonSerializer.DeserializeAsync<TestObject>(malformedJson))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*JSON content failed security validation*");
    }

    [TestMethod]
    public async Task ValidateJsonSecurityAsync_ValidJson_ReturnsTrue()
    {
        // Arrange
        var validJson = "{\"name\":\"Test\",\"value\":42}";

        // Act
        var result = await _secureJsonSerializer.ValidateJsonSecurityAsync(validJson);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task ValidateJsonSecurityAsync_JsonWithScriptTag_ReturnsFalse()
    {
        // Arrange
        var dangerousJson = "{\"content\":\"<script>alert('xss')</script>\"}";

        // Act
        var result = await _secureJsonSerializer.ValidateJsonSecurityAsync(dangerousJson);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task ValidateJsonSecurityAsync_JsonWithJavaScriptProtocol_ReturnsFalse()
    {
        // Arrange
        var dangerousJson = "{\"url\":\"javascript:alert('xss')\"}";

        // Act
        var result = await _secureJsonSerializer.ValidateJsonSecurityAsync(dangerousJson);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task ValidateJsonSecurityAsync_JsonWithEventHandler_ReturnsFalse()
    {
        // Arrange
        var dangerousJson = "{\"onclick\":\"maliciousFunction()\"}";

        // Act
        var result = await _secureJsonSerializer.ValidateJsonSecurityAsync(dangerousJson);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task ValidateJsonSecurityAsync_OversizedJson_ReturnsFalse()
    {
        // Arrange
        var largeString = new string('a', 51 * 1024 * 1024); // 51MB string
        var oversizedJson = $"{{\"data\":\"{largeString}\"}}";

        // Act
        var result = await _secureJsonSerializer.ValidateJsonSecurityAsync(oversizedJson);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task ValidateJsonSecurityAsync_DeeplyNestedJson_ReturnsFalse()
    {
        // Arrange - Create deeply nested JSON beyond the limit
        var deeplyNestedJson = "{";
        for (int i = 0; i < 70; i++) // Exceeds max depth of 64
        {
            deeplyNestedJson += "\"level" + i + "\":{";
        }
        deeplyNestedJson += "\"value\":\"deep\"";
        for (int i = 0; i < 70; i++)
        {
            deeplyNestedJson += "}";
        }
        deeplyNestedJson += "}";

        // Act
        var result = await _secureJsonSerializer.ValidateJsonSecurityAsync(deeplyNestedJson);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task SanitizeJsonAsync_JsonWithDangerousContent_RemovesDangerousPatterns()
    {
        // Arrange
        var dangerousJson = "{\"content\":\"<script>alert('xss')</script> and javascript:void(0)\"}";

        // Act
        var result = await _secureJsonSerializer.SanitizeJsonAsync(dangerousJson);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("javascript:");
        result.Should().Contain("[SANITIZED]");
    }

    [TestMethod]
    public async Task SanitizeJsonAsync_ValidJson_ReturnsUnchanged()
    {
        // Arrange
        var safeJson = "{\"name\":\"Test\",\"value\":42}";

        // Act
        var result = await _secureJsonSerializer.SanitizeJsonAsync(safeJson);

        // Assert
        result.Should().Be(safeJson);
    }

    [TestMethod]
    public async Task SerializeWithAuditAsync_ValidObject_ReturnsJsonAndLogsAudit()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 42 };
        var operationContext = "TestOperation";

        // Act
        var result = await _secureJsonSerializer.SerializeWithAuditAsync(testObject, operationContext);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"name\":");
        result.Should().Contain("\"value\":");

        // Verify audit logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Audited JSON serialization completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SerializeWithAuditAsync_NullOperationContext_ThrowsArgumentException()
    {
        // Arrange
        var testObject = new { Name = "Test" };

        // Act & Assert
        await FluentActions.Invoking(() => _secureJsonSerializer.SerializeWithAuditAsync(testObject, null!))
            .Should().ThrowAsync<ArgumentException>();

        await FluentActions.Invoking(() => _secureJsonSerializer.SerializeWithAuditAsync(testObject, ""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task DeserializeAsync_JsonWithUnicodeContent_HandlesCorrectly()
    {
        // Arrange
        var unicodeJson = "{\"name\":\"Test\\u0041\\u0042\",\"emoji\":\"\\uD83D\\uDE00\"}";

        // Act
        var result = await _secureJsonSerializer.DeserializeAsync<TestUnicodeObject>(unicodeJson);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("TestAB");
        result.Emoji.Should().Be("😀");
    }

    [TestMethod]
    public async Task ValidateJsonSecurityAsync_JsonWithNullBytes_ReturnsFalse()
    {
        // Arrange
        var jsonWithNullBytes = "{\"data\":\"test\\u0000content\"}";

        // Act
        var result = await _secureJsonSerializer.ValidateJsonSecurityAsync(jsonWithNullBytes);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task SerializeAsync_CustomOptions_UsesProvidedOptions()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 42 };
        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        // Act
        var result = await _secureJsonSerializer.SerializeAsync(testObject, customOptions);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"name\":");
        result.Should().Contain("\"value\":");
        result.Should().NotContain("  "); // Should not be indented
    }

    [TestMethod]
    public async Task ValidateJsonSecurityAsync_EmptyJson_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _secureJsonSerializer.ValidateJsonSecurityAsync(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ValidateJsonSecurityAsync_WhitespaceJson_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _secureJsonSerializer.ValidateJsonSecurityAsync("   "))
            .Should().ThrowAsync<ArgumentException>();
    }
}

/// <summary>
/// Test object for serialization tests.
/// </summary>
public class TestObject
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

/// <summary>
/// Test object for Unicode serialization tests.
/// </summary>
public class TestUnicodeObject
{
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
}
