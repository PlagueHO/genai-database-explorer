using FluentAssertions;
using GenAIDBExplorer.Core.PromptTemplates;
using Microsoft.Extensions.AI;

namespace GenAIDBExplorer.Core.Test.PromptTemplates;

[TestClass]
public class LiquidTemplateRendererTests
{
    private readonly LiquidTemplateRenderer _renderer = new();

    #region Render — Variable Substitution

    [TestMethod]
    public void Render_SimpleVariable_ShouldSubstitute()
    {
        // Arrange
        var template = "Hello, {{name}}!";
        var variables = new Dictionary<string, object?> { ["name"] = "World" };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Hello, World!");
    }

    [TestMethod]
    public void Render_MultipleVariables_ShouldSubstituteAll()
    {
        // Arrange
        var template = "DB: {{project_description}}\nTable: {{entity_structure}}";
        var variables = new Dictionary<string, object?>
        {
            ["project_description"] = "AdventureWorks CRM",
            ["entity_structure"] = "columns:\n- name: ID\n  type: int"
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Contain("AdventureWorks CRM");
        result.Should().Contain("columns:");
    }

    [TestMethod]
    public void Render_MissingVariable_ShouldProduceEmptyString()
    {
        // Arrange
        var template = "Value: {{missing_var}}";
        var variables = new Dictionary<string, object?>();

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Value: ");
    }

    [TestMethod]
    public void Render_NullVariableValue_ShouldProduceEmptyString()
    {
        // Arrange
        var template = "Value: {{nullable_var}}";
        var variables = new Dictionary<string, object?> { ["nullable_var"] = null };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Value: ");
    }

    #endregion

    #region Render — For Loops

    [TestMethod]
    public void Render_ForLoop_ShouldIterateOverCollection()
    {
        // Arrange
        var template = """
            {% for item in items %}
            - {{item.name}}
            {% endfor %}
            """;
        var variables = new Dictionary<string, object?>
        {
            ["items"] = new[]
            {
                new { name = "Alpha" },
                new { name = "Beta" },
                new { name = "Gamma" }
            }
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Contain("- Alpha");
        result.Should().Contain("- Beta");
        result.Should().Contain("- Gamma");
    }

    [TestMethod]
    public void Render_ForLoopWithObjectProperties_ShouldAccessNestedProperties()
    {
        // Arrange
        var template = """
            {% for table in tables %}
            ### Table [{{table.schema}}].[{{table.name}}]
            {{table.semanticdescription}}
            {% endfor %}
            """;
        var variables = new Dictionary<string, object?>
        {
            ["tables"] = new[]
            {
                new { schema = "SalesLT", name = "Product", semanticdescription = "Product table description." },
                new { schema = "SalesLT", name = "Customer", semanticdescription = "Customer table description." }
            }
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Contain("[SalesLT].[Product]");
        result.Should().Contain("Product table description.");
        result.Should().Contain("[SalesLT].[Customer]");
        result.Should().Contain("Customer table description.");
    }

    [TestMethod]
    public void Render_EmptyCollection_ShouldRenderNothing()
    {
        // Arrange
        var template = """
            {% for item in items %}
            - {{item.name}}
            {% endfor %}
            """;
        var variables = new Dictionary<string, object?> { ["items"] = Array.Empty<object>() };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Trim().Should().BeEmpty();
    }

    #endregion

    #region Render — Syntax Errors

    [TestMethod]
    public void Render_InvalidLiquidSyntax_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = "{% for item in %}broken{% endfor %}";
        var variables = new Dictionary<string, object?>();

        // Act
        var act = () => _renderer.Render(template, variables);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Liquid template syntax error*");
    }

    #endregion

    #region Render — Null Arguments

    [TestMethod]
    public void Render_NullTemplate_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _renderer.Render(null!, new Dictionary<string, object?>());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Render_NullVariables_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _renderer.Render("test", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region RenderMessages

    [TestMethod]
    public void RenderMessages_WithVariables_ShouldProduceChatMessages()
    {
        // Arrange
        var definition = new PromptTemplateDefinition(
            "test",
            "desc",
            new PromptTemplateModelParameters(0.1),
            [
                new PromptTemplateMessage(ChatRole.System, "You are a {{role}}."),
                new PromptTemplateMessage(ChatRole.User, "Tell me about {{topic}}.")
            ]
        );
        var variables = new Dictionary<string, object?>
        {
            ["role"] = "database assistant",
            ["topic"] = "SQL indexes"
        };

        // Act
        var messages = _renderer.RenderMessages(definition, variables);

        // Assert
        messages.Should().HaveCount(2);
        messages[0].Role.Should().Be(ChatRole.System);
        messages[0].Text.Should().Be("You are a database assistant.");
        messages[1].Role.Should().Be(ChatRole.User);
        messages[1].Text.Should().Be("Tell me about SQL indexes.");
    }

    [TestMethod]
    public void RenderMessages_NullDefinition_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _renderer.RenderMessages(null!, new Dictionary<string, object?>());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void RenderMessages_NullVariables_ShouldThrowArgumentNullException()
    {
        // Arrange
        var definition = new PromptTemplateDefinition("test", null, new PromptTemplateModelParameters(), []);

        // Act
        var act = () => _renderer.RenderMessages(definition, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void RenderMessages_FewShotMultiRole_ShouldRenderAllMessages()
    {
        // Arrange — simulates a few-shot prompt with system, user, assistant, user pattern
        var definition = new PromptTemplateDefinition(
            "few_shot",
            null,
            new PromptTemplateModelParameters(),
            [
                new PromptTemplateMessage(ChatRole.System, "You are a SQL assistant."),
                new PromptTemplateMessage(ChatRole.User, "Example: {{example_input}}"),
                new PromptTemplateMessage(ChatRole.Assistant, "Example response: {{example_output}}"),
                new PromptTemplateMessage(ChatRole.User, "Now answer: {{actual_input}}")
            ]
        );
        var variables = new Dictionary<string, object?>
        {
            ["example_input"] = "What tables exist?",
            ["example_output"] = "[SalesLT].[Product], [SalesLT].[Customer]",
            ["actual_input"] = "List all views."
        };

        // Act
        var messages = _renderer.RenderMessages(definition, variables);

        // Assert
        messages.Should().HaveCount(4);
        messages[0].Role.Should().Be(ChatRole.System);
        messages[1].Role.Should().Be(ChatRole.User);
        messages[1].Text.Should().Contain("What tables exist?");
        messages[2].Role.Should().Be(ChatRole.Assistant);
        messages[2].Text.Should().Contain("[SalesLT].[Product]");
        messages[3].Role.Should().Be(ChatRole.User);
        messages[3].Text.Should().Contain("List all views.");
    }

    #endregion

    #region Render — Plain Text Without Variables

    [TestMethod]
    public void Render_PlainTextWithoutVariables_ShouldReturnUnchanged()
    {
        // Arrange
        var template = "This is plain text with no variables.";
        var variables = new Dictionary<string, object?>();

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("This is plain text with no variables.");
    }

    #endregion
}
