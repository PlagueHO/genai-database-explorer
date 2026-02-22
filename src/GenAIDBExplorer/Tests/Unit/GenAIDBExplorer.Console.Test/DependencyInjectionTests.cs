using FluentAssertions;
using GenAIDBExplorer.Console.Extensions;
using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.PromptTemplates;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Console.Test;

[TestClass]
public class DependencyInjectionTests
{
    private static IHost BuildTestHost()
    {
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.ConfigureHost(Array.Empty<string>());
        return builder.Build();
    }

    [TestMethod]
    public void IChatClientFactory_IsRegistered()
    {
        // Arrange
        using var host = BuildTestHost();

        // Act
        var service = host.Services.GetService<IChatClientFactory>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<ChatClientFactory>();
    }

    [TestMethod]
    public void IPromptTemplateParser_IsRegistered()
    {
        // Arrange
        using var host = BuildTestHost();

        // Act
        var service = host.Services.GetService<IPromptTemplateParser>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<PromptTemplateParser>();
    }

    [TestMethod]
    public void ILiquidTemplateRenderer_IsRegistered()
    {
        // Arrange
        using var host = BuildTestHost();

        // Act
        var service = host.Services.GetService<ILiquidTemplateRenderer>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<LiquidTemplateRenderer>();
    }

    [TestMethod]
    public void IEmbeddingGenerator_IsRegistered_AsChatClientEmbeddingGenerator()
    {
        // Arrange
        using var host = BuildTestHost();

        // Act
        var service = host.Services.GetService<IEmbeddingGenerator>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<ChatClientEmbeddingGenerator>();
    }
}
