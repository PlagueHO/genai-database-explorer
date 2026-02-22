using FluentAssertions;
using GenAIDBExplorer.Core.PromptTemplates;
using Microsoft.Extensions.AI;

namespace GenAIDBExplorer.Core.Test.PromptTemplates;

[TestClass]
public class PromptTemplateDefinitionTests
{
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var messages = new List<PromptTemplateMessage>
        {
            new(ChatRole.System, "System prompt"),
            new(ChatRole.User, "User prompt")
        };
        var modelParams = new PromptTemplateModelParameters(Temperature: 0.1);

        // Act
        var definition = new PromptTemplateDefinition("test", "A test template", modelParams, messages);

        // Assert
        definition.Name.Should().Be("test");
        definition.Description.Should().Be("A test template");
        definition.ModelParameters.Should().Be(modelParams);
        definition.Messages.Should().HaveCount(2);
    }

    [TestMethod]
    public void Constructor_WithNullDescription_ShouldAllowNull()
    {
        // Arrange
        var messages = new List<PromptTemplateMessage>
        {
            new(ChatRole.User, "User prompt")
        };
        var modelParams = new PromptTemplateModelParameters();

        // Act
        var definition = new PromptTemplateDefinition("test", null, modelParams, messages);

        // Assert
        definition.Description.Should().BeNull();
    }

    [TestMethod]
    public void Messages_ShouldBeReadOnly()
    {
        // Arrange
        var messages = new List<PromptTemplateMessage>
        {
            new(ChatRole.System, "System prompt")
        };
        var definition = new PromptTemplateDefinition("test", null, new PromptTemplateModelParameters(), messages);

        // Assert
        definition.Messages.Should().BeAssignableTo<IReadOnlyList<PromptTemplateMessage>>();
    }

    [TestMethod]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var messages = new List<PromptTemplateMessage>
        {
            new(ChatRole.User, "User prompt")
        };
        var modelParams = new PromptTemplateModelParameters(Temperature: 0.5);
        var def1 = new PromptTemplateDefinition("test", "desc", modelParams, messages);
        var def2 = new PromptTemplateDefinition("test", "desc", modelParams, messages);

        // Assert
        def1.Should().Be(def2);
    }
}
