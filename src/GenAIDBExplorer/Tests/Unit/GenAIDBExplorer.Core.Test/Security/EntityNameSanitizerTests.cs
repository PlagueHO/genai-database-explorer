using FluentAssertions;
using GenAIDBExplorer.Core.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.Security;

[TestClass]
public class EntityNameSanitizerTests
{
    [TestMethod]
    public void SanitizeEntityName_ValidName_ReturnsUnchanged()
    {
        // Arrange
        var validName = "ValidEntityName";

        // Act
        var result = EntityNameSanitizer.SanitizeEntityName(validName);

        // Assert
        result.Should().Be(validName);
    }

    [TestMethod]
    public void SanitizeEntityName_NullName_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => EntityNameSanitizer.SanitizeEntityName(null!))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SanitizeEntityName_EmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => EntityNameSanitizer.SanitizeEntityName(string.Empty))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SanitizeEntityName_WhitespaceName_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => EntityNameSanitizer.SanitizeEntityName("   "))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SanitizeEntityName_TooLongName_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 129); // 129 characters, exceeds 128 limit

        // Act & Assert
        FluentActions.Invoking(() => EntityNameSanitizer.SanitizeEntityName(longName))
            .Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum length*");
    }

    [TestMethod]
    [DataRow("<test>", "_test_")]
    [DataRow("test:name", "test_name")]
    [DataRow("test\"name", "test_name")]
    [DataRow("test/name", "test_name")]
    [DataRow("test\\name", "test_name")]
    [DataRow("test|name", "test_name")]
    [DataRow("test?name", "test_name")]
    [DataRow("test*name", "test_name")]
    public void SanitizeEntityName_InvalidCharacters_ReplacesWithUnderscore(string input, string expected)
    {
        // Act
        var result = EntityNameSanitizer.SanitizeEntityName(input);

        // Assert
        result.Should().Be(expected);
    }

    [TestMethod]
    [DataRow(" test ", "test")]
    [DataRow(".test.", "test")]
    [DataRow(" .test. ", "test")]
    public void SanitizeEntityName_LeadingTrailingSpacesAndDots_TrimsCorrectly(string input, string expected)
    {
        // Act
        var result = EntityNameSanitizer.SanitizeEntityName(input);

        // Assert
        result.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("CON", "_CON")]
    [DataRow("PRN", "_PRN")]
    [DataRow("AUX", "_AUX")]
    [DataRow("NUL", "_NUL")]
    [DataRow("COM1", "_COM1")]
    [DataRow("LPT1", "_LPT1")]
    public void SanitizeEntityName_ReservedNames_PrependsUnderscore(string reservedName, string expected)
    {
        // Act
        var result = EntityNameSanitizer.SanitizeEntityName(reservedName);

        // Assert
        result.Should().Be(expected);
    }

    [TestMethod]
    public void SanitizeEntityName_ReservedNameCaseInsensitive_PrependsUnderscore()
    {
        // Act
        var result = EntityNameSanitizer.SanitizeEntityName("con");

        // Assert
        result.Should().Be("_con");
    }

    [TestMethod]
    public void SanitizeEntityName_OnlySpacesAndDots_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => EntityNameSanitizer.SanitizeEntityName(" . . "))
            .Should().Throw<ArgumentException>()
            .WithMessage("*results in empty string*");
    }

    [TestMethod]
    public void IsValidEntityName_ValidName_ReturnsTrue()
    {
        // Arrange
        var validName = "ValidEntityName123";

        // Act
        var result = EntityNameSanitizer.IsValidEntityName(validName);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void IsValidEntityName_InvalidNames_ReturnsFalse(string? invalidName)
    {
        // Act
        var result = EntityNameSanitizer.IsValidEntityName(invalidName!);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void IsValidEntityName_TooLongName_ReturnsFalse()
    {
        // Arrange
        var longName = new string('a', 129);

        // Act
        var result = EntityNameSanitizer.IsValidEntityName(longName);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("test<name")]
    [DataRow("test>name")]
    [DataRow("test:name")]
    [DataRow("test\"name")]
    [DataRow("test/name")]
    [DataRow("test\\name")]
    [DataRow("test|name")]
    [DataRow("test?name")]
    [DataRow("test*name")]
    public void IsValidEntityName_InvalidCharacters_ReturnsFalse(string invalidName)
    {
        // Act
        var result = EntityNameSanitizer.IsValidEntityName(invalidName);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(" test")]
    [DataRow("test ")]
    [DataRow(".test")]
    [DataRow("test.")]
    public void IsValidEntityName_LeadingTrailingSpacesOrDots_ReturnsFalse(string invalidName)
    {
        // Act
        var result = EntityNameSanitizer.IsValidEntityName(invalidName);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("CON")]
    [DataRow("PRN")]
    [DataRow("AUX")]
    [DataRow("NUL")]
    [DataRow("COM1")]
    [DataRow("LPT1")]
    public void IsValidEntityName_ReservedNames_ReturnsFalse(string reservedName)
    {
        // Act
        var result = EntityNameSanitizer.IsValidEntityName(reservedName);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void CreateSafeFileName_ValidInputs_ReturnsCorrectFileName()
    {
        // Arrange
        var schema = "dbo";
        var entityName = "TableName";

        // Act
        var result = EntityNameSanitizer.CreateSafeFileName(schema, entityName);

        // Assert
        result.Should().Be("dbo.TableName.json");
    }

    [TestMethod]
    public void CreateSafeFileName_CustomExtension_ReturnsCorrectFileName()
    {
        // Arrange
        var schema = "dbo";
        var entityName = "TableName";
        var extension = ".xml";

        // Act
        var result = EntityNameSanitizer.CreateSafeFileName(schema, entityName, extension);

        // Assert
        result.Should().Be("dbo.TableName.xml");
    }

    [TestMethod]
    public void CreateSafeFileName_ExtensionWithoutDot_AddsDoCorrectly()
    {
        // Arrange
        var schema = "dbo";
        var entityName = "TableName";
        var extension = "xml";

        // Act
        var result = EntityNameSanitizer.CreateSafeFileName(schema, entityName, extension);

        // Assert
        result.Should().Be("dbo.TableName.xml");
    }

    [TestMethod]
    public void CreateSafeFileName_InvalidCharacters_SanitizesCorrectly()
    {
        // Arrange
        var schema = "dbo<test>";
        var entityName = "Table:Name";

        // Act
        var result = EntityNameSanitizer.CreateSafeFileName(schema, entityName);

        // Assert
        result.Should().Be("dbo_test_.Table_Name.json");
    }

    [TestMethod]
    public void CreateSafeFileName_ReasonableLengthNames_GeneratesCorrectFileName()
    {
        // Arrange
        var schemaName = "TestSchema";
        var entityName = "TestEntity";

        // Act
        var result = EntityNameSanitizer.CreateSafeFileName(schemaName, entityName);

        // Assert
        result.Should().NotBeNull();
        result.Should().EndWith(".json");
        result.Should().Contain(".");
        result.Should().Be("TestSchema.TestEntity.json");
    }

    [TestMethod]
    public void CreateSafeFileName_NullSchemaName_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => EntityNameSanitizer.CreateSafeFileName(null!, "entity"))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void CreateSafeFileName_NullEntityName_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => EntityNameSanitizer.CreateSafeFileName("schema", null!))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void CreateSafeFileName_NullExtension_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => EntityNameSanitizer.CreateSafeFileName("schema", "entity", null!))
            .Should().Throw<ArgumentException>();
    }
}
