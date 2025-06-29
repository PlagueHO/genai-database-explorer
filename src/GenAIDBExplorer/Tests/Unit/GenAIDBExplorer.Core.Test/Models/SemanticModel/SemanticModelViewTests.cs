using System.Text;
using FluentAssertions;
using Moq;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel
{
    [TestClass]
    public class SemanticModelViewTests
    {
        [TestMethod]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            var schema = "dbo";
            var name = "TestView";
            var description = "Test description";

            // Act
            var view = new SemanticModelView(schema, name, description);

            // Assert
            view.Schema.Should().Be(schema);
            view.Name.Should().Be(name);
            view.Description.Should().Be(description);
            view.Columns.Should().NotBeNull();
            view.Columns.Should().BeEmpty();
            view.Definition.Should().BeEmpty();
            view.AdditionalInformation.Should().BeEmpty();
        }

        [TestMethod]
        public void AddColumn_ShouldAddColumnToColumnsList()
        {
            // Arrange
            var view = new SemanticModelView("dbo", "TestView");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description");

            // Act
            view.AddColumn(column);

            // Assert
            view.Columns.Should().ContainSingle()
                .Which.Should().Be(column);
        }

        [TestMethod]
        public void RemoveColumn_ShouldRemoveColumnFromColumnsList()
        {
            // Arrange
            var view = new SemanticModelView("dbo", "TestView");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description");
            view.Columns.Add(column);

            // Act
            var result = view.RemoveColumn(column);

            // Assert
            result.Should().BeTrue();
            view.Columns.Should().BeEmpty();
        }

        [TestMethod]
        public void RemoveColumn_WhenColumnNotInList_ShouldReturnFalse()
        {
            // Arrange
            var view = new SemanticModelView("dbo", "TestView");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description");

            // Act
            var result = view.RemoveColumn(column);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task LoadModelAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var view = new SemanticModelView("dbo", "NonExistentView");
            var folderPath = new DirectoryInfo("NonExistentDirectory");

            // Act
            Func<Task> act = async () => await view.LoadModelAsync(folderPath);

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [TestMethod]
        public void GetModelPath_ShouldReturnCorrectPath()
        {
            // Arrange
            var view = new SemanticModelView("dbo", "TestView");

            // Act
            var result = view.GetModelPath();

            // Assert
            result.Name.Should().Be("dbo.TestView.json");
            result.Parent.Name.Should().Be("views");
        }

        [TestMethod]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var view = new SemanticModelView("dbo", "TestView", "Test view description");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description") { Type = "int" };
            view.AddColumn(column);
            view.Definition = "SELECT * FROM dbo.TestTable";
            view.SemanticDescription = "This is a semantic description.";

            var expected = new StringBuilder();
            expected.AppendLine("Entity: [dbo].[TestView]");
            expected.AppendLine("Description:");
            expected.AppendLine("Test view description");
            expected.AppendLine();
            expected.AppendLine("Columns:");
            expected.AppendLine("  - TestColumn (int)");
            expected.AppendLine("    Description: Test column description");
            expected.AppendLine();
            expected.AppendLine("Definition:");
            expected.AppendLine("SELECT * FROM dbo.TestTable");
            expected.AppendLine();
            expected.AppendLine("Semantic Description:");
            expected.AppendLine("This is a semantic description.");

            // Act
            var result = view.ToString();

            // Assert
            result.Should().Be(expected.ToString());
        }

        [TestMethod]
        public void Accept_ShouldInvokeVisitorMethods()
        {
            // Arrange
            var view = new SemanticModelView("dbo", "TestView");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description");
            view.AddColumn(column);

            var mockVisitor = new Mock<ISemanticModelVisitor>();

            // Act
            view.Accept(mockVisitor.Object);

            // Assert
            mockVisitor.Verify(v => v.VisitView(view), Times.Once);
            mockVisitor.Verify(v => v.VisitColumn(column), Times.Once);
        }
    }
}
