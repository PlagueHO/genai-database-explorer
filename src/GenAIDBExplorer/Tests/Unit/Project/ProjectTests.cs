using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Tests.Unit.Project
{
    [TestClass]
    public class ProjectTests
    {
        private Mock<ILogger<Project>> _loggerMock;
        private string _testRoot;
        private string _defaultProjectPath;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<Project>>();
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
            // Arrange
            var projectDir = new DirectoryInfo(Path.Combine(_testRoot, "NewProject"));
            var project = new Project(_loggerMock.Object);
            // Place a dummy file in DefaultProject to verify copy
            File.WriteAllText(Path.Combine(_defaultProjectPath, "dummy.txt"), "test");
            // Patch AppContext.BaseDirectory to _testRoot for this test
            var baseDirField = typeof(AppContext).GetField("s_baseDirectory", BindingFlags.NonPublic | BindingFlags.Static);
            baseDirField?.SetValue(null, _testRoot);

            // Act
            project.InitializeProjectDirectory(projectDir);

            // Assert
            projectDir.Exists.Should().BeTrue();
            File.Exists(Path.Combine(projectDir.FullName, "dummy.txt")).Should().BeTrue();
        }

        [TestMethod]
        public void InitializeProjectDirectory_ThrowsIfDirectoryNotEmpty()
        {
            // Arrange
            var projectDir = new DirectoryInfo(Path.Combine(_testRoot, "NotEmptyProject"));
            projectDir.Create();
            File.WriteAllText(Path.Combine(projectDir.FullName, "file.txt"), "data");
            var project = new Project(_loggerMock.Object);
            // Patch AppContext.BaseDirectory to _testRoot for this test
            var baseDirField = typeof(AppContext).GetField("s_baseDirectory", BindingFlags.NonPublic | BindingFlags.Static);
            baseDirField?.SetValue(null, _testRoot);

            // Act
            Action act = () => project.InitializeProjectDirectory(projectDir);

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void InitializeProjectDirectory_ThrowsIfDefaultProjectMissing()
        {
            // Arrange
            var projectDir = new DirectoryInfo(Path.Combine(_testRoot, "MissingDefaultProject"));
            var project = new Project(_loggerMock.Object);
            // Remove DefaultProject directory
            Directory.Delete(_defaultProjectPath, true);
            // Patch AppContext.BaseDirectory to _testRoot for this test
            var baseDirField = typeof(AppContext).GetField("s_baseDirectory", BindingFlags.NonPublic | BindingFlags.Static);
            baseDirField?.SetValue(null, _testRoot);

            // Act
            Action act = () => project.InitializeProjectDirectory(projectDir);

            // Assert
            act.Should().Throw<DirectoryNotFoundException>();
        }
    }
}
