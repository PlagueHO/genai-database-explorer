using System.Text;
using FluentAssertions;
using Moq;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel
{
    [TestClass]
    public class SemanticModelTableTests
    {
        [TestMethod]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            var schema = "dbo";
            var name = "TestTable";
            var description = "Test description";

            // Act
            var table = new SemanticModelTable(schema, name, description);

            // Assert
            table.Schema.Should().Be(schema);
            table.Name.Should().Be(name);
            table.Description.Should().Be(description);
            table.Details.Should().BeEmpty();
            table.AdditionalInformation.Should().BeEmpty();
            table.Columns.Should().NotBeNull();
            table.Columns.Should().BeEmpty();
            table.Indexes.Should().NotBeNull();
            table.Indexes.Should().BeEmpty();
        }

        [TestMethod]
        public void AddColumn_ShouldAddColumnToList()
        {
            // Arrange
            var table = new SemanticModelTable("dbo", "TestTable");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description");

            // Act
            table.AddColumn(column);

            // Assert
            table.Columns.Should().ContainSingle()
                .Which.Should().Be(column);
        }

        [TestMethod]
        public void RemoveColumn_ShouldRemoveColumnFromList()
        {
            // Arrange
            var table = new SemanticModelTable("dbo", "TestTable");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description");
            table.Columns.Add(column);

            // Act
            var result = table.RemoveColumn(column);

            // Assert
            result.Should().BeTrue();
            table.Columns.Should().BeEmpty();
        }

        [TestMethod]
        public void RemoveColumn_ShouldReturnFalseWhenColumnNotInList()
        {
            // Arrange
            var table = new SemanticModelTable("dbo", "TestTable");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description");

            // Act
            var result = table.RemoveColumn(column);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void AddIndex_ShouldAddIndexToList()
        {
            // Arrange
            var table = new SemanticModelTable("dbo", "TestTable");
            var index = new SemanticModelIndex("dbo", "TestIndex", "Test index description");

            // Act
            table.AddIndex(index);

            // Assert
            table.Indexes.Should().ContainSingle()
                .Which.Should().Be(index);
        }

        [TestMethod]
        public void RemoveIndex_ShouldRemoveIndexFromList()
        {
            // Arrange
            var table = new SemanticModelTable("dbo", "TestTable");
            var index = new SemanticModelIndex("dbo", "TestIndex", "Test index description");
            table.Indexes.Add(index);

            // Act
            var result = table.RemoveIndex(index);

            // Assert
            result.Should().BeTrue();
            table.Indexes.Should().BeEmpty();
        }

        [TestMethod]
        public void RemoveIndex_ShouldReturnFalseWhenIndexNotInList()
        {
            // Arrange
            var table = new SemanticModelTable("dbo", "TestTable");
            var index = new SemanticModelIndex("dbo", "TestIndex", "Test index description");

            // Act
            var result = table.RemoveIndex(index);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ToString_ShouldReturnExpectedString()
        {
            // Arrange
            var table = new SemanticModelTable("dbo", "TestTable", "Test table description");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description") { Type = "int" };
            table.AddColumn(column);
            var index = new SemanticModelIndex("dbo", "TestIndex", "Test index description");
            table.AddIndex(index);
            table.SemanticDescription = "This is a semantic description.";
            table.Details = "These are the details of the table from the data dictionary.";
            table.AdditionalInformation = "This is additional information about the table.";

            var expected = new StringBuilder();
            expected.AppendLine("Entity: [dbo].[TestTable]");
            expected.AppendLine("Description:");
            expected.AppendLine("Test table description");
            expected.AppendLine();
            expected.AppendLine("Details:");
            expected.AppendLine("These are the details of the table from the data dictionary.");
            expected.AppendLine();
            expected.AppendLine("Columns:");
            expected.AppendLine("  - TestColumn (int)");
            expected.AppendLine("    Description: Test column description");
            expected.AppendLine();
            expected.AppendLine("Indexes:");
            expected.AppendLine("  - TestIndex");
            expected.AppendLine();
            expected.AppendLine("Additional Information:");
            expected.AppendLine("This is additional information about the table.");
            expected.AppendLine();
            expected.AppendLine("Semantic Description:");
            expected.AppendLine("This is a semantic description.");

            // Act
            var result = table.ToString();

            // Assert
            result.Should().Be(expected.ToString());
        }

        [TestMethod]
        public void Accept_ShouldInvokeVisitorMethods()
        {
            // Arrange
            var table = new SemanticModelTable("dbo", "TestTable");
            var column = new SemanticModelColumn("dbo", "TestColumn", "Test column description");
            table.AddColumn(column);

            var mockVisitor = new Mock<ISemanticModelVisitor>();

            // Act
            table.Accept(mockVisitor.Object);

            // Assert
            mockVisitor.Verify(v => v.VisitTable(table), Times.Once);
            mockVisitor.Verify(v => v.VisitColumn(column), Times.Once);
        }

        [TestMethod]
        public void GetModelPath_ShouldReturnExpectedDirectoryInfo()
        {
            // Arrange
            var table = new SemanticModelTable("dbo", "TestTable");

            // Act
            var result = table.GetModelPath();

            // Assert
            result.Name.Should().Be("dbo.TestTable.json");
            result.Parent.Name.Should().Be("tables");
        }
    }
}
