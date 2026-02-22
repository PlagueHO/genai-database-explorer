using FluentAssertions;
using GenAIDBExplorer.Core.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace GenAIDBExplorer.Core.Test.Security
{
    [TestClass]
    public class EnhancedSecurityValidationTests
    {
        [TestMethod]
        public void PathValidator_ValidateAndSanitizePath_ValidPath_ShouldReturnPath()
        {
            // Arrange
            var validPath = Path.Combine(Path.GetTempPath(), "valid", "path", "to", "file.txt");

            // Act & Assert
            var result = PathValidator.ValidateAndSanitizePath(validPath);
            result.Should().NotBeNull();
            result.Should().Be(Path.GetFullPath(validPath));
        }

        [TestMethod]
        public void PathValidator_ValidateAndSanitizePath_PathWithDangerousSegments_ShouldThrow()
        {
            // Arrange
            var dangerousPath = Path.Combine(Path.GetTempPath(), "..", "dangerous", "path");

            // Act & Assert
            FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(dangerousPath))
                .Should().Throw<ArgumentException>()
                .WithMessage("*dangerous segment*");
        }

        [TestMethod]
        public void PathValidator_ValidateAndSanitizePath_EmptyPath_ShouldThrow()
        {
            // Act & Assert
            FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(""))
                .Should().Throw<ArgumentException>();

            FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(null!))
                .Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void PathValidator_ValidateAndSanitizePath_RelativePath_ShouldThrow()
        {
            // Arrange
            var relativePath = "relative\\path\\to\\file";

            // Act & Assert
            FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(relativePath))
                .Should().Throw<ArgumentException>()
                .WithMessage("*must be an absolute path*");
        }

        [TestMethod]
        public void PathValidator_IsPathWithinDirectory_ValidChild_ShouldReturnTrue()
        {
            // Arrange
            var parentPath = Path.GetTempPath();
            var childPath = Path.Combine(parentPath, "subfolder", "file.txt");

            // Act
            var result = PathValidator.IsPathWithinDirectory(parentPath, childPath);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void PathValidator_IsPathWithinDirectory_PathTraversal_ShouldReturnFalse()
        {
            // Arrange
            var parentPath = Path.Combine(Path.GetTempPath(), "parent");
            var maliciousPath = Path.Combine(parentPath, "..", "..", "system32");

            // Act
            var result = PathValidator.IsPathWithinDirectory(parentPath, maliciousPath);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void PathValidator_ValidateDirectoryPath_ValidPath_ShouldReturnDirectoryInfo()
        {
            // Arrange
            var validPath = Path.GetTempPath();

            // Act
            var result = PathValidator.ValidateDirectoryPath(validPath);

            // Assert
            result.Should().NotBeNull();
            result.FullName.Should().Be(Path.GetFullPath(validPath));
        }

        [TestMethod]
        public void EntityNameSanitizer_SanitizeEntityName_ValidName_ShouldReturnSameName()
        {
            // Arrange
            var validName = "ValidTableName";

            // Act
            var result = EntityNameSanitizer.SanitizeEntityName(validName);

            // Assert
            result.Should().Be(validName);
        }

        [TestMethod]
        public void EntityNameSanitizer_SanitizeEntityName_StrictMode_InvalidCharacters_ShouldSanitize()
        {
            // Arrange
            var invalidName = "Table<>:invalid|chars";

            // Act
            var result = EntityNameSanitizer.SanitizeEntityName(invalidName, strictMode: true);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotContain("<");
            result.Should().NotContain(">");
            result.Should().NotContain(":");
            result.Should().NotContain("|");
            result.Should().Be("Table___invalid_chars");
        }

        [TestMethod]
        public void EntityNameSanitizer_SanitizeEntityName_NonStrictMode_ShouldSanitize()
        {
            // Arrange
            var invalidName = "Table<>:invalid|chars";

            // Act
            var result = EntityNameSanitizer.SanitizeEntityName(invalidName, strictMode: false);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotContain("<");
            result.Should().NotContain(">");
            result.Should().NotContain(":");
            result.Should().NotContain("|");
        }

        [TestMethod]
        public void EntityNameSanitizer_SanitizeEntityName_EmptyName_ShouldThrow()
        {
            // Act & Assert
            FluentActions.Invoking(() => EntityNameSanitizer.SanitizeEntityName(""))
                .Should().Throw<ArgumentException>();

            FluentActions.Invoking(() => EntityNameSanitizer.SanitizeEntityName(null!))
                .Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void EntityNameSanitizer_IsValidEntityName_ValidName_ShouldReturnTrue()
        {
            // Arrange
            var validName = "ValidTableName";

            // Act
            var result = EntityNameSanitizer.IsValidEntityName(validName);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void EntityNameSanitizer_IsValidEntityName_InvalidName_ShouldReturnFalse()
        {
            // Arrange
            var invalidName = "Table<>:file"; // Using actual invalid file characters

            // Act
            var result = EntityNameSanitizer.IsValidEntityName(invalidName, strictMode: true);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void EntityNameSanitizer_ValidateInputSecurity_DangerousInput_ShouldThrow()
        {
            // Arrange
            var dangerousInput = "<script>alert('xss')</script>";

            // Act & Assert
            FluentActions.Invoking(() => EntityNameSanitizer.ValidateInputSecurity(dangerousInput, "testParam"))
                .Should().Throw<ArgumentException>()
                .WithMessage("*dangerous content*");
        }

        [TestMethod]
        public void EntityNameSanitizer_ValidateInputSecurity_ValidInput_ShouldNotThrow()
        {
            // Arrange
            var validInput = "ValidEntityName123";

            // Act & Assert
            FluentActions.Invoking(() => EntityNameSanitizer.ValidateInputSecurity(validInput, "testParam"))
                .Should().NotThrow();
        }

        [TestMethod]
        [DataRow("ValidName123")]
        [DataRow("_ValidName")]
        [DataRow("ValidName_123")]
        public void EntityNameSanitizer_SanitizeEntityName_ValidNames_ShouldPass(string validName)
        {
            // Act
            var result = EntityNameSanitizer.SanitizeEntityName(validName);

            // Assert
            result.Should().Be(validName);
        }

        [TestMethod]
        [DataRow("CON")]
        [DataRow("PRN")]
        [DataRow("AUX")]
        [DataRow("NUL")]
        [DataRow("COM1")]
        [DataRow("LPT1")]
        public void PathValidator_ValidateAndSanitizePath_ReservedNames_ShouldThrow(string reservedName)
        {
            // Arrange
            var pathWithReservedName = Path.Combine(Path.GetTempPath(), reservedName);

            // Act & Assert
            FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(pathWithReservedName))
                .Should().Throw<ArgumentException>()
                .WithMessage("*reserved device name*");
        }

        [TestMethod]
        [DataRow("<")]
        [DataRow(">")]
        [DataRow("\"")]
        [DataRow("|")]
        [DataRow("?")]
        [DataRow("*")]
        public void PathValidator_ValidateAndSanitizePath_InvalidCharacters_ShouldThrow(string invalidChar)
        {
            // Arrange
            var pathWithInvalidChar = Path.Combine(Path.GetTempPath(), $"test{invalidChar}path");

            // Act & Assert
            FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(pathWithInvalidChar))
                .Should().Throw<ArgumentException>()
                .WithMessage("*invalid characters*");
        }

        [TestMethod]
        public void PathValidator_IsPathSafeForConcurrentOperations_ValidPath_ShouldReturnTrue()
        {
            // Arrange
            var validPath = Path.GetTempPath();

            // Act
            var result = PathValidator.IsPathSafeForConcurrentOperations(validPath);

            // Assert
            result.Should().BeTrue();
        }
    }
}