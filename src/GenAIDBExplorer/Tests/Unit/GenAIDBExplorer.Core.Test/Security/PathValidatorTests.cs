using System;
using System.IO;
using FluentAssertions;
using GenAIDBExplorer.Core.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.Security;

[TestClass]
public class PathValidatorTests
{
    [TestMethod]
    public void ValidateAndSanitizePath_ValidAbsolutePath_ReturnsFullPath()
    {
        // Arrange
        var tempPath = Path.GetTempPath();
        var testPath = Path.Combine(tempPath, "test", "valid");

        // Act
        var result = PathValidator.ValidateAndSanitizePath(testPath);

        // Assert
        result.Should().Be(Path.GetFullPath(testPath));
    }

    [TestMethod]
    public void ValidateAndSanitizePath_NullPath_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(null!))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void ValidateAndSanitizePath_EmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(string.Empty))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void ValidateAndSanitizePath_WhitespacePath_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath("   "))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void ValidateAndSanitizePath_RelativePath_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath("relative/path"))
            .Should().Throw<ArgumentException>()
            .WithMessage("*must be an absolute path*");
    }

    [TestMethod]
    public void ValidateAndSanitizePath_PathTraversalAttempt_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = Path.Combine(Path.GetTempPath(), "..", "system32");

        // Act & Assert
        FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(maliciousPath))
            .Should().Throw<ArgumentException>()
            .WithMessage("*dangerous segment*");
    }

    [TestMethod]
    [DataRow("<")]
    [DataRow(">")]
    [DataRow(":")]
    [DataRow("\"")]
    [DataRow("|")]
    [DataRow("?")]
    [DataRow("*")]
    public void ValidateAndSanitizePath_InvalidCharacters_ThrowsArgumentException(string invalidChar)
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), $"test{invalidChar}path");

        // Act & Assert
        FluentActions.Invoking(() => PathValidator.ValidateAndSanitizePath(invalidPath))
            .Should().Throw<ArgumentException>()
            .WithMessage("*invalid characters*");
    }

    [TestMethod]
    public void IsPathWithinDirectory_ValidChildPath_ReturnsTrue()
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
    public void IsPathWithinDirectory_PathTraversalAttempt_ReturnsFalse()
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
    public void IsPathWithinDirectory_SamePath_ReturnsTrue()
    {
        // Arrange
        var path = Path.GetTempPath();

        // Act
        var result = PathValidator.IsPathWithinDirectory(path, path);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public void IsPathWithinDirectory_NullParentPath_ThrowsArgumentException()
    {
        // Arrange
        var childPath = Path.GetTempPath();

        // Act & Assert
        FluentActions.Invoking(() => PathValidator.IsPathWithinDirectory(null!, childPath))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void IsPathWithinDirectory_NullChildPath_ThrowsArgumentException()
    {
        // Arrange
        var parentPath = Path.GetTempPath();

        // Act & Assert
        FluentActions.Invoking(() => PathValidator.IsPathWithinDirectory(parentPath, null!))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void ValidateDirectoryPath_ValidPath_ReturnsDirectoryInfo()
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
    public void ValidateDirectoryPath_InvalidPath_ThrowsArgumentException()
    {
        // Arrange
        var invalidPath = "relative/path";

        // Act & Assert
        FluentActions.Invoking(() => PathValidator.ValidateDirectoryPath(invalidPath))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void ValidateDirectoryPath_PathWithInvalidCharacters_ThrowsArgumentException()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), "test<>path");

        // Act & Assert
        FluentActions.Invoking(() => PathValidator.ValidateDirectoryPath(invalidPath))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void ValidateDirectoryPath_NonExistentParent_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "subfolder", "target");

        // Act & Assert
        FluentActions.Invoking(() => PathValidator.ValidateDirectoryPath(nonExistentPath))
            .Should().Throw<DirectoryNotFoundException>();
    }
}
