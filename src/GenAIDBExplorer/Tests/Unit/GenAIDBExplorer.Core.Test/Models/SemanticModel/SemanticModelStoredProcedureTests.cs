using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel
{
    [TestClass]
    public class SemanticModelStoredProcedureTests
    {
        [TestMethod]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            var schema = "dbo";
            var name = "TestStoredProcedure";
            var definition = "CREATE PROCEDURE dbo.TestStoredProcedure AS SELECT 1";
            var parameters = "@Param1 INT";
            var description = "Test stored procedure description";

            // Act
            var storedProcedure = new SemanticModelStoredProcedure(schema, name, definition, parameters, description);

            // Assert
            storedProcedure.Schema.Should().Be(schema);
            storedProcedure.Name.Should().Be(name);
            storedProcedure.Definition.Should().Be(definition);
            storedProcedure.Parameters.Should().Be(parameters);
            storedProcedure.Description.Should().Be(description);
            storedProcedure.AdditionalInformation.Should().BeEmpty();
            storedProcedure.SemanticDescription.Should().BeNull();
            storedProcedure.NotUsed.Should().BeFalse();
            storedProcedure.NotUsedReason.Should().BeNull();
        }

        [TestMethod]
        public void GetModelPath_ShouldReturnCorrectPath()
        {
            // Arrange
            var storedProcedure = new SemanticModelStoredProcedure("dbo", "TestStoredProcedure", "Definition");

            // Act
            var result = storedProcedure.GetModelPath();

            // Assert
            result.Name.Should().Be("dbo.TestStoredProcedure.json");
            result.Parent.Name.Should().Be("storedprocedures");
        }

        [TestMethod]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var storedProcedure = new SemanticModelStoredProcedure(
                "dbo",
                "TestStoredProcedure",
                "CREATE PROCEDURE dbo.TestStoredProcedure AS SELECT 1",
                "@Param1 INT",
                "Test stored procedure description"
            );
            storedProcedure.SemanticDescription = "This is a semantic description.";

            var expected = new StringBuilder();
            expected.AppendLine("Entity: [dbo].[TestStoredProcedure]");
            expected.AppendLine("Description:");
            expected.AppendLine("Test stored procedure description");
            expected.AppendLine();
            expected.AppendLine("Parameters:");
            expected.AppendLine("@Param1 INT");
            expected.AppendLine();
            expected.AppendLine("Definition:");
            expected.AppendLine("CREATE PROCEDURE dbo.TestStoredProcedure AS SELECT 1");
            expected.AppendLine();
            expected.AppendLine("Description:");
            expected.AppendLine("Test stored procedure description");
            expected.AppendLine();
            expected.AppendLine("Semantic Description:");
            expected.AppendLine("This is a semantic description.");

            // Act
            var result = storedProcedure.ToString();

            // Assert
            result.Should().Be(expected.ToString());
        }

        [TestMethod]
        public void SetSemanticDescription_ShouldUpdateSemanticDescription()
        {
            // Arrange
            var storedProcedure = new SemanticModelStoredProcedure("dbo", "TestStoredProcedure", "Definition");
            var semanticDescription = "This is a semantic description.";

            // Act
            storedProcedure.SetSemanticDescription(semanticDescription);

            // Assert
            storedProcedure.SemanticDescription.Should().Be(semanticDescription);
            storedProcedure.SemanticDescriptionLastUpdate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public async Task LoadModelAsync_WhenFileExists_ShouldLoadProperties()
        {
            // Arrange
            var schema = "dbo";
            var name = "TestStoredProcedure";
            var definition = "CREATE PROCEDURE dbo.TestStoredProcedure AS SELECT 1";
            var parameters = "@Param1 INT";
            var description = "Test stored procedure description";
            var storedProcedure = new SemanticModelStoredProcedure(schema, name, definition, parameters, description);

            var folderPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            folderPath.Create();

            await storedProcedure.SaveModelAsync(folderPath);

            var storedProcedureToLoad = new SemanticModelStoredProcedure(schema, name, definition);

            // Act
            await storedProcedureToLoad.LoadModelAsync(folderPath);

            // Assert
            storedProcedureToLoad.Schema.Should().Be(schema);
            storedProcedureToLoad.Name.Should().Be(name);
            storedProcedureToLoad.Definition.Should().Be(definition);
            storedProcedureToLoad.Parameters.Should().Be(parameters);
            storedProcedureToLoad.Description.Should().Be(description);

            // Clean up
            folderPath.Delete(true);
        }

        [TestMethod]
        public async Task LoadModelAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var storedProcedure = new SemanticModelStoredProcedure("dbo", "NonExistentSP", "Definition");
            var folderPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            // Act
            Func<Task> act = async () => await storedProcedure.LoadModelAsync(folderPath);

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [TestMethod]
        public void Accept_ShouldInvokeVisitorMethod()
        {
            // Arrange
            var storedProcedure = new SemanticModelStoredProcedure("dbo", "TestStoredProcedure", "Definition");
            var mockVisitor = new Mock<ISemanticModelVisitor>();

            // Act
            storedProcedure.Accept(mockVisitor.Object);

            // Assert
            mockVisitor.Verify(v => v.VisitStoredProcedure(storedProcedure), Times.Once);
        }
    }
}
