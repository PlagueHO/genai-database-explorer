using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Core.Test.Models.Project;

[TestClass]
public class ProjectTests
{
    private Mock<ILogger<GenAIDBExplorer.Core.Models.Project.Project>> _loggerMock = null!;
    private string _testRoot = null!;
    private string _defaultProjectPath = null!;

    // Static lock to prevent tests from interfering with each other
    private static readonly object _defaultProjectLock = new object();

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<GenAIDBExplorer.Core.Models.Project.Project>>();
        _testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRoot);
        _defaultProjectPath = Path.Combine(_testRoot, "DefaultProject");
        Directory.CreateDirectory(_defaultProjectPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, true);
    }

    [TestMethod]
    public void InitializeProjectDirectory_CreatesDirectoryIfNotExists()
    {
        lock (_defaultProjectLock)
        {
            // Arrange
            var projectDir = new DirectoryInfo(Path.Combine(_testRoot, "NewProject"));
            var project = new GenAIDBExplorer.Core.Models.Project.Project(_loggerMock.Object);

            // This test uses the existing DefaultProject in the app directory
            // We just need to ensure it exists for the test to pass
            var appBaseDir = AppContext.BaseDirectory;
            var defaultProjectPath = Path.Combine(appBaseDir, "DefaultProject");

            // Create a minimal DefaultProject if it doesn't exist
            var needsCleanup = false;
            if (!Directory.Exists(defaultProjectPath))
            {
                needsCleanup = true;
                Directory.CreateDirectory(defaultProjectPath);

                var genaiDbExplorerDir = Path.Combine(defaultProjectPath, ".genaidbexplorer");
                Directory.CreateDirectory(genaiDbExplorerDir);

                // Create minimal settings.json
                var settingsContent = """
                {
                  "ConnectionString": "",
                  "DatabaseName": "TestDatabase",
                  "OutputFormats": ["json", "markdown"],
                  "MaxRetries": 3,
                  "RetryDelay": "00:00:05"
                }
                """;
                File.WriteAllText(Path.Combine(defaultProjectPath, "settings.json"), settingsContent);

                // Create version.json
                var versionContent = """
                {
                  "version": "1.0.0",
                  "created": "2024-01-01T00:00:00Z"
                }
                """;
                File.WriteAllText(Path.Combine(genaiDbExplorerDir, "version.json"), versionContent);
            }

            try
            {
                // Act
                project.InitializeProjectDirectory(projectDir);

                // Assert
                projectDir.Exists.Should().BeTrue();
                File.Exists(Path.Combine(projectDir.FullName, "settings.json")).Should().BeTrue();
                Directory.Exists(Path.Combine(projectDir.FullName, ".genaidbexplorer")).Should().BeTrue();
                File.Exists(Path.Combine(projectDir.FullName, ".genaidbexplorer", "version.json")).Should().BeTrue();
            }
            finally
            {
                // Clean up the temporary DefaultProject if we created it
                if (needsCleanup && Directory.Exists(defaultProjectPath))
                {
                    try
                    {
                        Directory.Delete(defaultProjectPath, true);
                    }
                    catch (IOException)
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }

    [TestMethod]
    public void InitializeProjectDirectory_ThrowsIfDirectoryNotEmpty()
    {
        // Arrange
        var projectDir = new DirectoryInfo(Path.Combine(_testRoot, "NotEmptyProject"));
        projectDir.Create();
        File.WriteAllText(Path.Combine(projectDir.FullName, "file.txt"), "data");
        var project = new GenAIDBExplorer.Core.Models.Project.Project(_loggerMock.Object);

        // This test doesn't need DefaultProject to exist to test the directory not empty condition
        // The method should check if the directory is empty before trying to access DefaultProject

        // Act
        Action act = () => project.InitializeProjectDirectory(projectDir);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void InitializeProjectDirectory_ThrowsIfDefaultProjectMissing()
    {
        lock (_defaultProjectLock)
        {
            // Arrange
            var projectDir = new DirectoryInfo(Path.Combine(_testRoot, "MissingDefaultProject"));
            var project = new GenAIDBExplorer.Core.Models.Project.Project(_loggerMock.Object);

            // This test verifies that the method properly handles the case where DefaultProject is missing
            var originalBaseDirectory = AppContext.BaseDirectory;
            var defaultProjectPath = Path.Combine(originalBaseDirectory, "DefaultProject");
            var defaultProjectBackupPath = defaultProjectPath + "_test_backup_" + Guid.NewGuid().ToString("N")[..8];

            // Check if DefaultProject exists and back it up temporarily
            bool defaultProjectExisted = Directory.Exists(defaultProjectPath);

            try
            {
                if (defaultProjectExisted)
                {
                    // Move the DefaultProject temporarily to simulate it being missing
                    Directory.Move(defaultProjectPath, defaultProjectBackupPath);
                }

                // Verify DefaultProject doesn't exist
                Directory.Exists(defaultProjectPath).Should().BeFalse("DefaultProject should not exist for this test");

                // Act & Assert
                Action act = () => project.InitializeProjectDirectory(projectDir);
                act.Should().ThrowExactly<DirectoryNotFoundException>()
                    .WithMessage("*DefaultProject directory not found*");
            }
            finally
            {
                // Restore the DefaultProject if it existed before
                if (defaultProjectExisted && Directory.Exists(defaultProjectBackupPath))
                {
                    try
                    {
                        if (Directory.Exists(defaultProjectPath))
                        {
                            Directory.Delete(defaultProjectPath, true);
                        }
                        Directory.Move(defaultProjectBackupPath, defaultProjectPath);
                    }
                    catch (IOException)
                    {
                        // If restore fails, try to clean up the backup at least
                        try { Directory.Delete(defaultProjectBackupPath, true); } catch { }
                    }
                }
            }
        }
    }
}
