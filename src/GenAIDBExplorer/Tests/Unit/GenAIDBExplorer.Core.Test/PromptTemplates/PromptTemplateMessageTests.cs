using FluentAssertions;
using GenAIDBExplorer.Core.PromptTemplates;
using Microsoft.Extensions.AI;

namespace GenAIDBExplorer.Core.Test.PromptTemplates;

[TestClass]
public class PromptTemplateMessageTests
{
    [TestMethod]
    public void Constructor_WithSystemRole_ShouldCreateInstance()
    {
        // Act
        var message = new PromptTemplateMessage(ChatRole.System, "You are a helpful assistant.");

        // Assert
        message.Role.Should().Be(ChatRole.System);
        message.ContentTemplate.Should().Be("You are a helpful assistant.");
    }

    [TestMethod]
    public void Constructor_WithUserRole_ShouldCreateInstance()
    {
        // Act
        var message = new PromptTemplateMessage(ChatRole.User, "Hello {{name}}");

        // Assert
        message.Role.Should().Be(ChatRole.User);
        message.ContentTemplate.Should().Be("Hello {{name}}");
    }

    [TestMethod]
    public void Constructor_WithAssistantRole_ShouldCreateInstance()
    {
        // Act
        var message = new PromptTemplateMessage(ChatRole.Assistant, "Response text");

        // Assert
        message.Role.Should().Be(ChatRole.Assistant);
        message.ContentTemplate.Should().Be("Response text");
    }

    [TestMethod]
    public void Constructor_WithEmptyContentTemplate_ShouldBeAllowed()
    {
        // Act
        var message = new PromptTemplateMessage(ChatRole.User, string.Empty);

        // Assert
        message.ContentTemplate.Should().BeEmpty();
    }

    [TestMethod]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var msg1 = new PromptTemplateMessage(ChatRole.System, "Hello");
        var msg2 = new PromptTemplateMessage(ChatRole.System, "Hello");

        // Assert
        msg1.Should().Be(msg2);
    }

    [TestMethod]
    public void RecordEquality_WithDifferentRoles_ShouldNotBeEqual()
    {
        // Arrange
        var msg1 = new PromptTemplateMessage(ChatRole.System, "Hello");
        var msg2 = new PromptTemplateMessage(ChatRole.User, "Hello");

        // Assert
        msg1.Should().NotBe(msg2);
    }
}
