using FluentAssertions;
using GenAIDBExplorer.Core.PromptTemplates;
using Microsoft.Extensions.AI;

namespace GenAIDBExplorer.Core.Test.PromptTemplates;

/// <summary>
/// Parity tests verifying that each of the 6 .prompt files can be parsed and rendered
/// with known inputs, producing output that matches expected golden results (SC-002).
/// </summary>
[TestClass]
public class PromptTemplateParityTests
{
    private readonly PromptTemplateParser _parser = new();
    private readonly LiquidTemplateRenderer _renderer = new();

    private static string GetPromptTemplatesDir()
    {
        // Navigate from test output (Tests/Unit/GenAIDBExplorer.Core.Test/bin/Debug/net10.0)
        // up 6 levels to src/GenAIDBExplorer/, then into GenAIDBExplorer.Core/PromptTemplates/
        var baseDir = AppContext.BaseDirectory;
        var corePromptDir = Path.GetFullPath(
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "GenAIDBExplorer.Core", "PromptTemplates"));
        return corePromptDir;
    }

    #region describe_semanticmodeltable.prompt

    [TestMethod]
    public void DescribeSemanticModelTable_ShouldParseCorrectMetadata()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodeltable.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert
        result.Name.Should().Be("semantic_model_describe_table");
        result.Description.Should().Be("Generate a description for a SQL table.");
        result.ModelParameters.Temperature.Should().Be(0.1);
    }

    [TestMethod]
    public void DescribeSemanticModelTable_ShouldHaveCorrectRoles()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodeltable.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert — system, user (example), assistant (example), user (template)
        result.Messages.Should().HaveCount(4);
        result.Messages[0].Role.Should().Be(ChatRole.System);
        result.Messages[1].Role.Should().Be(ChatRole.User);
        result.Messages[2].Role.Should().Be(ChatRole.Assistant);
        result.Messages[3].Role.Should().Be(ChatRole.User);
    }

    [TestMethod]
    public void DescribeSemanticModelTable_ShouldRenderTemplateVariables()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodeltable.prompt");
        var definition = _parser.ParseFromFile(path);
        var variables = new Dictionary<string, object?>
        {
            ["project_description"] = "Test database for unit tests.",
            ["entity_structure"] = "schema: TestSchema\nname: TestTable",
            ["entity_data"] = "[{\"id\":1}]"
        };

        // Act — render only the last user message (the template)
        var lastUserMessage = definition.Messages[^1];
        var rendered = _renderer.Render(lastUserMessage.ContentTemplate, variables);

        // Assert
        rendered.Should().Contain("Test database for unit tests.");
        rendered.Should().Contain("schema: TestSchema");
        rendered.Should().Contain("[{\"id\":1}]");
    }

    #endregion

    #region describe_semanticmodelview.prompt

    [TestMethod]
    public void DescribeSemanticModelView_ShouldParseCorrectMetadata()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodelview.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert
        result.Name.Should().Be("semantic_model_describe_view");
        result.Description.Should().Be("Generate a description for a SQL view.");
        result.ModelParameters.Temperature.Should().Be(0.1);
    }

    [TestMethod]
    public void DescribeSemanticModelView_ShouldHaveCorrectRoles()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodelview.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert — system, user (example), assistant (example), user (template with for-loop)
        result.Messages.Should().HaveCount(4);
        result.Messages[0].Role.Should().Be(ChatRole.System);
        result.Messages[1].Role.Should().Be(ChatRole.User);
        result.Messages[2].Role.Should().Be(ChatRole.Assistant);
        result.Messages[3].Role.Should().Be(ChatRole.User);
    }

    [TestMethod]
    public void DescribeSemanticModelView_ShouldRenderForLoopInTemplate()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodelview.prompt");
        var definition = _parser.ParseFromFile(path);
        var variables = new Dictionary<string, object?>
        {
            ["project_description"] = "Test DB",
            ["entity_structure"] = "definition: CREATE VIEW ...",
            ["entity_data"] = "[]",
            ["tables"] = new[]
            {
                new { schema = "SalesLT", name = "Product", semanticdescription = "Product table." },
                new { schema = "SalesLT", name = "Customer", semanticdescription = "Customer table." }
            }
        };

        // Act
        var lastUserMessage = definition.Messages[^1];
        var rendered = _renderer.Render(lastUserMessage.ContentTemplate, variables);

        // Assert
        rendered.Should().Contain("[SalesLT].[Product]");
        rendered.Should().Contain("Product table.");
        rendered.Should().Contain("[SalesLT].[Customer]");
        rendered.Should().Contain("Customer table.");
    }

    #endregion

    #region describe_semanticmodelstoredprocedure.prompt

    [TestMethod]
    public void DescribeSemanticModelStoredProcedure_ShouldParseCorrectMetadata()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodelstoredprocedure.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert
        result.Name.Should().Be("semantic_model_describe_stored_procedure");
        result.Description.Should().Be("Generate a description for a SQL stored procedure.");
        result.ModelParameters.Temperature.Should().Be(0.1);
    }

    [TestMethod]
    public void DescribeSemanticModelStoredProcedure_ShouldHaveCorrectRoles()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodelstoredprocedure.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert — system, user (example), assistant (example), user (template with for-loop)
        result.Messages.Should().HaveCount(4);
        result.Messages[0].Role.Should().Be(ChatRole.System);
        result.Messages[1].Role.Should().Be(ChatRole.User);
        result.Messages[2].Role.Should().Be(ChatRole.Assistant);
        result.Messages[3].Role.Should().Be(ChatRole.User);
    }

    [TestMethod]
    public void DescribeSemanticModelStoredProcedure_ShouldRenderForLoopInTemplate()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodelstoredprocedure.prompt");
        var definition = _parser.ParseFromFile(path);
        var variables = new Dictionary<string, object?>
        {
            ["project_description"] = "Test DB",
            ["entity_definition"] = "CREATE PROCEDURE ...",
            ["entity_parameters"] = "@Param1 INT",
            ["tables"] = new[]
            {
                new { schema = "dbo", name = "Orders", semanticdescription = "Orders table." }
            }
        };

        // Act
        var lastUserMessage = definition.Messages[^1];
        var rendered = _renderer.Render(lastUserMessage.ContentTemplate, variables);

        // Assert
        rendered.Should().Contain("Test DB");
        rendered.Should().Contain("CREATE PROCEDURE ...");
        rendered.Should().Contain("@Param1 INT");
        rendered.Should().Contain("[dbo].[Orders]");
        rendered.Should().Contain("Orders table.");
    }

    #endregion

    #region get_table_from_data_dictionary_markdown.prompt

    [TestMethod]
    public void GetTableFromDataDictionaryMarkdown_ShouldParseCorrectMetadata()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "get_table_from_data_dictionary_markdown.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert
        result.Name.Should().Be("get_table_from_data_dictionary_markdown");
        result.Description.Should().Be("Get a table from a data dictionary markdown file.");
        result.ModelParameters.Temperature.Should().Be(0.1);
    }

    [TestMethod]
    public void GetTableFromDataDictionaryMarkdown_ShouldHaveCorrectRoles()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "get_table_from_data_dictionary_markdown.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert — system, user
        result.Messages.Should().HaveCount(2);
        result.Messages[0].Role.Should().Be(ChatRole.System);
        result.Messages[1].Role.Should().Be(ChatRole.User);
    }

    [TestMethod]
    public void GetTableFromDataDictionaryMarkdown_ShouldRenderTemplateVariables()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "get_table_from_data_dictionary_markdown.prompt");
        var definition = _parser.ParseFromFile(path);
        var variables = new Dictionary<string, object?>
        {
            ["entity_markdown"] = "## Table dbo.Orders\n| Column | Type |\n|--------|------|\n| OrderID | int |"
        };

        // Act
        var lastUserMessage = definition.Messages[^1];
        var rendered = _renderer.Render(lastUserMessage.ContentTemplate, variables);

        // Assert
        rendered.Should().Contain("## Table dbo.Orders");
        rendered.Should().Contain("OrderID");
    }

    #endregion

    #region get_tables_from_view_definition.prompt

    [TestMethod]
    public void GetTablesFromViewDefinition_ShouldParseCorrectMetadata()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "get_tables_from_view_definition.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert
        result.Name.Should().Be("get_tables_from_view_definition");
        result.Description.Should().Be("Get a list of the tables used in a SQL view definition.");
        result.ModelParameters.Temperature.Should().Be(0.1);
    }

    [TestMethod]
    public void GetTablesFromViewDefinition_ShouldHaveCorrectRoles()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "get_tables_from_view_definition.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert — system, user
        result.Messages.Should().HaveCount(2);
        result.Messages[0].Role.Should().Be(ChatRole.System);
        result.Messages[1].Role.Should().Be(ChatRole.User);
    }

    [TestMethod]
    public void GetTablesFromViewDefinition_ShouldRenderTemplateVariables()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "get_tables_from_view_definition.prompt");
        var definition = _parser.ParseFromFile(path);
        var variables = new Dictionary<string, object?>
        {
            ["entity_definition"] = "CREATE VIEW [SalesLT].[vProductAndDescription] AS SELECT ..."
        };

        // Act
        var lastUserMessage = definition.Messages[^1];
        var rendered = _renderer.Render(lastUserMessage.ContentTemplate, variables);

        // Assert
        rendered.Should().Contain("CREATE VIEW [SalesLT].[vProductAndDescription]");
    }

    #endregion

    #region get_tables_from_storedprocedure_definition.prompt

    [TestMethod]
    public void GetTablesFromStoredProcedureDefinition_ShouldParseCorrectMetadata()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "get_tables_from_storedprocedure_definition.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert
        result.Name.Should().Be("get_tables_from_stored_procedure_definition");
        result.Description.Should().Be("Get a list of the tables used in a SQL stored procedure definition.");
        result.ModelParameters.Temperature.Should().Be(0.1);
    }

    [TestMethod]
    public void GetTablesFromStoredProcedureDefinition_ShouldHaveCorrectRoles()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "get_tables_from_storedprocedure_definition.prompt");

        // Act
        var result = _parser.ParseFromFile(path);

        // Assert — system, user
        result.Messages.Should().HaveCount(2);
        result.Messages[0].Role.Should().Be(ChatRole.System);
        result.Messages[1].Role.Should().Be(ChatRole.User);
    }

    [TestMethod]
    public void GetTablesFromStoredProcedureDefinition_ShouldRenderTemplateVariables()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "get_tables_from_storedprocedure_definition.prompt");
        var definition = _parser.ParseFromFile(path);
        var variables = new Dictionary<string, object?>
        {
            ["entity_definition"] = "CREATE PROCEDURE [dbo].[GetOrders] AS BEGIN SELECT * FROM dbo.Orders END"
        };

        // Act
        var lastUserMessage = definition.Messages[^1];
        var rendered = _renderer.Render(lastUserMessage.ContentTemplate, variables);

        // Assert
        rendered.Should().Contain("CREATE PROCEDURE [dbo].[GetOrders]");
    }

    #endregion

    #region Full Pipeline — Parse + Render Messages (End-to-End)

    [TestMethod]
    public void DescribeSemanticModelTable_FullPipeline_ShouldRenderAllMessages()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodeltable.prompt");
        var definition = _parser.ParseFromFile(path);
        var variables = new Dictionary<string, object?>
        {
            ["project_description"] = "Test CRM database.",
            ["entity_structure"] = "schema: dbo\nname: TestTable",
            ["entity_data"] = "[{\"id\":1,\"name\":\"test\"}]"
        };

        // Act
        var messages = _renderer.RenderMessages(definition, variables);

        // Assert — 4 messages: system, user (example, unchanged), assistant (example, unchanged), user (rendered)
        messages.Should().HaveCount(4);
        messages[0].Role.Should().Be(ChatRole.System);
        messages[1].Role.Should().Be(ChatRole.User);
        messages[2].Role.Should().Be(ChatRole.Assistant);
        messages[3].Role.Should().Be(ChatRole.User);
        // The last user message should have variables rendered
        messages[3].Text.Should().Contain("Test CRM database.");
        messages[3].Text.Should().Contain("schema: dbo");
        messages[3].Text.Should().Contain("[{\"id\":1,\"name\":\"test\"}]");
    }

    [TestMethod]
    public void DescribeSemanticModelView_FullPipeline_ShouldRenderForLoops()
    {
        // Arrange
        var path = Path.Combine(GetPromptTemplatesDir(), "describe_semanticmodelview.prompt");
        var definition = _parser.ParseFromFile(path);
        var variables = new Dictionary<string, object?>
        {
            ["project_description"] = "Test DB",
            ["entity_structure"] = "definition: CREATE VIEW ...",
            ["entity_data"] = "[]",
            ["tables"] = new[]
            {
                new { schema = "SalesLT", name = "Product", semanticdescription = "Product info." }
            }
        };

        // Act
        var messages = _renderer.RenderMessages(definition, variables);

        // Assert
        messages.Should().HaveCount(4);
        var lastMessage = messages[^1];
        lastMessage.Text.Should().Contain("[SalesLT].[Product]");
        lastMessage.Text.Should().Contain("Product info.");
    }

    #endregion
}
