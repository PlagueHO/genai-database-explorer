using FluentAssertions;
using GenAIDBExplorer.Core.PromptTemplates;

namespace GenAIDBExplorer.Core.Test.PromptTemplates;

[TestClass]
public class PromptTemplateModelParametersTests
{
    [TestMethod]
    public void Constructor_WithDefaults_ShouldHaveNullValues()
    {
        // Act
        var parameters = new PromptTemplateModelParameters();

        // Assert
        parameters.Temperature.Should().BeNull();
        parameters.TopP.Should().BeNull();
        parameters.MaxTokens.Should().BeNull();
    }

    [TestMethod]
    public void Constructor_WithTemperature_ShouldSetValue()
    {
        // Act
        var parameters = new PromptTemplateModelParameters(Temperature: 0.7);

        // Assert
        parameters.Temperature.Should().Be(0.7);
        parameters.TopP.Should().BeNull();
        parameters.MaxTokens.Should().BeNull();
    }

    [TestMethod]
    public void Constructor_WithAllParameters_ShouldSetAllValues()
    {
        // Act
        var parameters = new PromptTemplateModelParameters(Temperature: 0.5, TopP: 0.9, MaxTokens: 1024);

        // Assert
        parameters.Temperature.Should().Be(0.5);
        parameters.TopP.Should().Be(0.9);
        parameters.MaxTokens.Should().Be(1024);
    }

    [TestMethod]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var p1 = new PromptTemplateModelParameters(Temperature: 0.1, TopP: 0.9, MaxTokens: 500);
        var p2 = new PromptTemplateModelParameters(Temperature: 0.1, TopP: 0.9, MaxTokens: 500);

        // Assert
        p1.Should().Be(p2);
    }

    [TestMethod]
    public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var p1 = new PromptTemplateModelParameters(Temperature: 0.1);
        var p2 = new PromptTemplateModelParameters(Temperature: 0.9);

        // Assert
        p1.Should().NotBe(p2);
    }
}
