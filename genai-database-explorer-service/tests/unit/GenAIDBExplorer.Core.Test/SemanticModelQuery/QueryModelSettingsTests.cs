using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.SemanticModelQuery;

[TestClass]
public class QueryModelSettingsTests
{
    [TestMethod]
    public void Constructor_DefaultValues_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var settings = new QueryModelSettings();

        // Assert
        settings.AgentName.Should().Be("genaidb-query-agent");
        settings.AgentInstructions.Should().BeNull();
        settings.MaxResponseRounds.Should().Be(10);
        settings.MaxTokenBudget.Should().Be(100_000);
        settings.TimeoutSeconds.Should().Be(60);
        settings.DefaultTopK.Should().Be(5);
    }

    [TestMethod]
    public void PropertyName_ShouldBeQueryModel()
    {
        // Assert
        QueryModelSettings.PropertyName.Should().Be("QueryModel");
    }

    [TestMethod]
    public void Properties_WhenSet_ShouldReturnSetValues()
    {
        // Arrange
        var settings = new QueryModelSettings
        {
            AgentName = "custom-agent",
            AgentInstructions = "Custom instructions",
            MaxResponseRounds = 20,
            MaxTokenBudget = 200_000,
            TimeoutSeconds = 120,
            DefaultTopK = 10
        };

        // Assert
        settings.AgentName.Should().Be("custom-agent");
        settings.AgentInstructions.Should().Be("Custom instructions");
        settings.MaxResponseRounds.Should().Be(20);
        settings.MaxTokenBudget.Should().Be(200_000);
        settings.TimeoutSeconds.Should().Be(120);
        settings.DefaultTopK.Should().Be(10);
    }
}
