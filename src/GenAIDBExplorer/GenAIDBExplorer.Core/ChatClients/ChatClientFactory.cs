using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace GenAIDBExplorer.Core.ChatClients;

/// <summary>
/// Factory for creating AI chat clients and embedding generators from project configuration.
/// Uses <see cref="OpenAIClient"/> with endpoint override for Azure AI Foundry,
/// supporting both Entra ID and API key authentication.
/// This approach supports any model hosted in Azure AI Foundry (OpenAI, DeepSeek, Meta, etc.).
/// </summary>
public sealed class ChatClientFactory(
    IProject project,
    ILogger<ChatClientFactory> logger
) : IChatClientFactory
{
    private readonly IProject _project = project;
    private readonly ILogger<ChatClientFactory> _logger = logger;

    /// <summary>
    /// The Azure AI Foundry token audience scope for Entra ID authentication.
    /// </summary>
    private const string FoundryTokenScope = "https://ai.azure.com/.default";

    /// <inheritdoc />
    public IChatClient CreateChatClient()
    {
        var defaultSettings = _project.Settings.FoundryModels.Default;
        var chatSettings = _project.Settings.FoundryModels.ChatCompletion;

        var deploymentName = chatSettings.DeploymentName
            ?? throw new InvalidOperationException("ChatCompletion deployment name is required.");

        _logger.LogDebug("Creating chat client with deployment: {DeploymentName}", deploymentName);

        var client = CreateOpenAIClient(defaultSettings);
        return client.GetChatClient(deploymentName).AsIChatClient();
    }

    /// <inheritdoc />
    public IChatClient CreateStructuredOutputChatClient()
    {
        var defaultSettings = _project.Settings.FoundryModels.Default;
        var chatSettings = _project.Settings.FoundryModels.ChatCompletionStructured;

        var deploymentName = chatSettings.DeploymentName
            ?? throw new InvalidOperationException("ChatCompletionStructured deployment name is required.");

        _logger.LogDebug("Creating structured output chat client with deployment: {DeploymentName}", deploymentName);

        var client = CreateOpenAIClient(defaultSettings);
        return client.GetChatClient(deploymentName).AsIChatClient();
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator()
    {
        var defaultSettings = _project.Settings.FoundryModels.Default;
        var embeddingSettings = _project.Settings.FoundryModels.Embedding;

        var deploymentName = embeddingSettings.DeploymentName
            ?? throw new InvalidOperationException("Embedding deployment name is required.");

        _logger.LogDebug("Creating embedding generator with deployment: {DeploymentName}", deploymentName);

        var client = CreateOpenAIClient(defaultSettings);
        return client.GetEmbeddingClient(deploymentName).AsIEmbeddingGenerator();
    }

    private OpenAIClient CreateOpenAIClient(FoundryModelsDefaultSettings defaultSettings)
    {
        var endpoint = defaultSettings.Endpoint
            ?? throw new InvalidOperationException("Foundry Models endpoint is required.");

        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };

        if (defaultSettings.AuthenticationType == AuthenticationType.EntraIdAuthentication)
        {
            var credential = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = defaultSettings.TenantId })
                : new DefaultAzureCredential();

            var tenantInfo = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                ? $" for tenant {defaultSettings.TenantId}"
                : string.Empty;
            _logger.LogInformation(
                "Using Microsoft Entra ID Default authentication for Foundry Models{TenantInfo}.",
                tenantInfo);

#pragma warning disable OPENAI001 // OpenAIClient(AuthenticationPolicy, OpenAIClientOptions) is experimental; this is the Microsoft-recommended Foundry auth pattern
            return new OpenAIClient(
                new BearerTokenPolicy(credential, FoundryTokenScope),
                clientOptions);
#pragma warning restore OPENAI001
        }
        else
        {
            var apiKey = defaultSettings.ApiKey
                ?? throw new InvalidOperationException("API key is required when using ApiKey authentication.");

            _logger.LogInformation("Using API key authentication for Foundry Models.");

            return new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        }
    }
}
