using System.Data;
using FluentAssertions;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Data.DatabaseProviders
{
    [TestClass]
    public class SqlConnectionProviderTests
    {
        private Mock<IProject> _mockProject;
        private Mock<ILogger<SqlConnectionProvider>> _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockProject = new Mock<IProject>();
            _mockLogger = new Mock<ILogger<SqlConnectionProvider>>();
        }

        [TestMethod]
        public async Task ConnectAsync_WithMissingConnectionString_ShouldThrowInvalidDataException()
        {
            // Arrange
            var projectSettings = new ProjectSettings
            {
                Database = new DatabaseSettings { ConnectionString = null },
                DataDictionary = new DataDictionarySettings(),
                SemanticModel = new SemanticModelSettings(),
                OpenAIService = new OpenAIServiceSettings()
            };
            _mockProject.Setup(p => p.Settings).Returns(projectSettings);

            var provider = new SqlConnectionProvider(_mockProject.Object, _mockLogger.Object);

            // Act
            Func<Task> act = async () => await provider.ConnectAsync();

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("Missing database connection string.");
        }

        [TestMethod]
        public async Task ConnectAsync_WhenGeneralExceptionOccurs_ShouldThrowException()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=master;Trusted_Connection=True;";
            var projectSettings = new ProjectSettings
            {
                Database = new DatabaseSettings { ConnectionString = connectionString },
                DataDictionary = new DataDictionarySettings(),
                SemanticModel = new SemanticModelSettings(),
                OpenAIService = new OpenAIServiceSettings()
            };
            _mockProject.Setup(p => p.Settings).Returns(projectSettings);

            // Simulate a general exception during connection.OpenAsync()
            var provider = new SqlConnectionProvider(_mockProject.Object, _mockLogger.Object);

            // Act
            Func<Task> act = async () =>
            {
                await provider.ConnectAsync();
            };

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }
    }
}
