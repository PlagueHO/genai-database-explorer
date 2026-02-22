using FluentAssertions;
using GenAIDBExplorer.Core.PromptTemplates;
using Microsoft.Extensions.AI;

namespace GenAIDBExplorer.Core.Test.PromptTemplates;

[TestClass]
public class PromptTemplateParserTests
{
    private readonly PromptTemplateParser _parser = new();

    #region Parse — YAML Extraction

    [TestMethod]
    public void Parse_ValidYamlFrontmatter_ShouldExtractName()
    {
        // Arrange
        var content = """
            ---
            name: my_template
            description: A test template
            model:
              api: chat
              parameters:
                temperature: 0.5
            ---
            system:
            Hello world
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Name.Should().Be("my_template");
    }

    [TestMethod]
    public void Parse_ValidYamlFrontmatter_ShouldExtractDescription()
    {
        // Arrange
        var content = """
            ---
            name: test
            description: My detailed description
            model:
              api: chat
              parameters:
                temperature: 0.1
            ---
            system:
            Content here
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Description.Should().Be("My detailed description");
    }

    [TestMethod]
    public void Parse_YamlWithModelParameters_ShouldExtractTemperature()
    {
        // Arrange
        var content = """
            ---
            name: test
            model:
              parameters:
                temperature: 0.7
            ---
            system:
            Content
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.ModelParameters.Temperature.Should().Be(0.7);
    }

    [TestMethod]
    public void Parse_YamlWithModelParameters_ShouldExtractTopP()
    {
        // Arrange
        var content = """
            ---
            name: test
            model:
              parameters:
                top_p: 0.95
            ---
            system:
            Content
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.ModelParameters.TopP.Should().Be(0.95);
    }

    [TestMethod]
    public void Parse_YamlWithModelParameters_ShouldExtractMaxTokens()
    {
        // Arrange
        var content = """
            ---
            name: test
            model:
              parameters:
                max_tokens: 4096
            ---
            system:
            Content
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.ModelParameters.MaxTokens.Should().Be(4096);
    }

    [TestMethod]
    public void Parse_YamlWithoutModelParameters_ShouldReturnNullParameters()
    {
        // Arrange
        var content = """
            ---
            name: test
            ---
            system:
            Content
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.ModelParameters.Temperature.Should().BeNull();
        result.ModelParameters.TopP.Should().BeNull();
        result.ModelParameters.MaxTokens.Should().BeNull();
    }

    [TestMethod]
    public void Parse_YamlWithoutDescription_ShouldReturnNullDescription()
    {
        // Arrange
        var content = """
            ---
            name: test
            ---
            system:
            Content
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Description.Should().BeNull();
    }

    #endregion

    #region Parse — Role Parsing

    [TestMethod]
    public void Parse_SystemAndUserRoles_ShouldExtractBothMessages()
    {
        // Arrange
        var content = """
            ---
            name: test
            ---
            system:
            You are an assistant.
            user:
            Hello there.
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Messages.Should().HaveCount(2);
        result.Messages[0].Role.Should().Be(ChatRole.System);
        result.Messages[0].ContentTemplate.Should().Contain("You are an assistant.");
        result.Messages[1].Role.Should().Be(ChatRole.User);
        result.Messages[1].ContentTemplate.Should().Contain("Hello there.");
    }

    [TestMethod]
    public void Parse_AllThreeRoles_ShouldExtractThreeMessages()
    {
        // Arrange
        var content = """
            ---
            name: test
            ---
            system:
            System message.
            user:
            User message.
            assistant:
            Assistant message.
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Messages.Should().HaveCount(3);
        result.Messages[0].Role.Should().Be(ChatRole.System);
        result.Messages[1].Role.Should().Be(ChatRole.User);
        result.Messages[2].Role.Should().Be(ChatRole.Assistant);
    }

    [TestMethod]
    public void Parse_MultiLineContent_ShouldPreserveNewlines()
    {
        // Arrange
        var content = """
            ---
            name: test
            ---
            system:
            Line one.
            Line two.
            Line three.
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Messages.Should().HaveCount(1);
        result.Messages[0].ContentTemplate.Should().Contain("Line one.");
        result.Messages[0].ContentTemplate.Should().Contain("Line two.");
        result.Messages[0].ContentTemplate.Should().Contain("Line three.");
    }

    [TestMethod]
    public void Parse_RepeatedRoles_ShouldCreateSeparateMessages()
    {
        // Arrange
        var content = """
            ---
            name: test
            ---
            system:
            System prompt.
            user:
            First user message.
            assistant:
            First assistant response.
            user:
            Second user message.
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Messages.Should().HaveCount(4);
        result.Messages[0].Role.Should().Be(ChatRole.System);
        result.Messages[1].Role.Should().Be(ChatRole.User);
        result.Messages[1].ContentTemplate.Should().Contain("First user");
        result.Messages[2].Role.Should().Be(ChatRole.Assistant);
        result.Messages[3].Role.Should().Be(ChatRole.User);
        result.Messages[3].ContentTemplate.Should().Contain("Second user");
    }

    #endregion

    #region Parse — Edge Cases

    [TestMethod]
    public void Parse_NoRoleMarkers_ShouldReturnEmptyMessages()
    {
        // Arrange
        var content = """
            ---
            name: test
            ---
            This body has no role markers at all.
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Messages.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_MalformedYaml_MissingClosingDelimiter_ShouldThrowFormatException()
    {
        // Arrange
        var content = """
            ---
            name: test
            description: Missing closing delimiter
            system:
            Content here
            """;

        // Act
        var act = () => _parser.Parse(content);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*closing*delimiter*");
    }

    [TestMethod]
    public void Parse_NoYamlFrontmatter_ShouldThrowFormatException()
    {
        // Arrange
        var content = """
            system:
            Content without YAML frontmatter.
            """;

        // Act
        var act = () => _parser.Parse(content);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*YAML frontmatter*");
    }

    [TestMethod]
    public void Parse_NullContent_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _parser.Parse(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Parse_EmptyYamlBody_ShouldThrowFormatException()
    {
        // Arrange
        var content = "---\n\n---\n";

        // Act
        var act = () => _parser.Parse(content);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*YAML frontmatter is empty*");
    }

    #endregion

    #region ParseFromFile

    [TestMethod]
    public void ParseFromFile_MissingFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.prompt");

        // Act
        var act = () => _parser.ParseFromFile(nonExistentPath);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [TestMethod]
    public void ParseFromFile_ValidFile_ShouldParseCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var content = "---\nname: file_test\ndescription: From file\nmodel:\n  parameters:\n    temperature: 0.3\n---\nsystem:\nYou are helpful.\nuser:\n{{question}}\n";
            File.WriteAllText(tempFile, content);

            // Act
            var result = _parser.ParseFromFile(tempFile);

            // Assert
            result.Name.Should().Be("file_test");
            result.Description.Should().Be("From file");
            result.ModelParameters.Temperature.Should().Be(0.3);
            result.Messages.Should().HaveCount(2);
            result.Messages[0].Role.Should().Be(ChatRole.System);
            result.Messages[1].Role.Should().Be(ChatRole.User);
            result.Messages[1].ContentTemplate.Should().Contain("{{question}}");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region Parse — Liquid Template Variables Preservation

    [TestMethod]
    public void Parse_ContentWithLiquidVariables_ShouldPreserveTemplateVariables()
    {
        // Arrange
        var content = """
            ---
            name: test
            ---
            system:
            You are a database assistant.
            user:
            # Database Purpose
            {{project_description}}

            # Table Structure
            {{entity_structure}}
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Messages[1].ContentTemplate.Should().Contain("{{project_description}}");
        result.Messages[1].ContentTemplate.Should().Contain("{{entity_structure}}");
    }

    [TestMethod]
    public void Parse_ContentWithLiquidForLoops_ShouldPreserveLoopSyntax()
    {
        // Arrange
        var content = """
            ---
            name: test
            ---
            user:
            {% for table in tables %}
            ### Table [{{table.schema}}].[{{table.name}}]
            {{table.semanticdescription}}
            {% endfor %}
            """;

        // Act
        var result = _parser.Parse(content);

        // Assert
        result.Messages[0].ContentTemplate.Should().Contain("{% for table in tables %}");
        result.Messages[0].ContentTemplate.Should().Contain("{% endfor %}");
        result.Messages[0].ContentTemplate.Should().Contain("{{table.schema}}");
    }

    #endregion
}
