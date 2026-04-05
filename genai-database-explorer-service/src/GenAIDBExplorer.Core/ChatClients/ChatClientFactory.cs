using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Projects;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.ChatClients;

/// <summary>
/// Factory for creating AI chat clients and embedding generators from project configuration.
/// Uses <see cref="AIProjectClient"/> to connect through a Microsoft Foundry project endpoint,
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

    private AIProjectClient? _projectClient;
    private ProjectOpenAIClient? _openAIClient;
    private readonly object _lock = new();

    /// <inheritdoc />
    public IChatClient CreateChatClient()
    {
        var chatSettings = _project.Settings.MicrosoftFoundry.ChatCompletion;

        var deploymentName = chatSettings.DeploymentName
            ?? throw new InvalidOperationException("ChatCompletion deployment name is required.");

        _logger.LogDebug("Creating chat client with deployment: {DeploymentName}", deploymentName);

        var openAIClient = GetOrCreateOpenAIClient();
        return openAIClient.GetChatClient(deploymentName).AsIChatClient();
    }

    /// <inheritdoc />
    public IChatClient CreateStructuredOutputChatClient()
    {
        _logger.LogDebug("Creating structured output chat client (delegates to ChatCompletion deployment).");
        return CreateChatClient();
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator()
    {
        var embeddingSettings = _project.Settings.MicrosoftFoundry.Embedding;

        var deploymentName = embeddingSettings.DeploymentName
            ?? throw new InvalidOperationException("Embedding deployment name is required.");

        _logger.LogDebug("Creating embedding generator with deployment: {DeploymentName}", deploymentName);

        var openAIClient = GetOrCreateOpenAIClient();
        return openAIClient.GetEmbeddingClient(deploymentName).AsIEmbeddingGenerator();
    }

    /// <inheritdoc />
    public AIProjectClient GetProjectClient()
    {
        GetOrCreateOpenAIClient();
        return _projectClient
            ?? throw new InvalidOperationException(
                "AIProjectClient is only available with Entra ID authentication. " +
                "API key authentication does not support Foundry project operations such as agent hosting.");
    }

    private ProjectOpenAIClient GetOrCreateOpenAIClient()
    {
        if (_openAIClient is not null) return _openAIClient;

        lock (_lock)
        {
            if (_openAIClient is not null) return _openAIClient;

            var defaultSettings = _project.Settings.MicrosoftFoundry.Default;
            var endpoint = defaultSettings.Endpoint
                ?? throw new InvalidOperationException("Microsoft Foundry endpoint is required.");

            var endpointUri = new Uri(endpoint);

            if (defaultSettings.AuthenticationType == AuthenticationType.EntraIdAuthentication)
            {
                var credential = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                    ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = defaultSettings.TenantId })
                    : new DefaultAzureCredential();

                var tenantInfo = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                    ? $" for tenant {defaultSettings.TenantId}"
                    : string.Empty;
                _logger.LogInformation(
                    "Creating AIProjectClient with Entra ID authentication for {Endpoint}{TenantInfo}.",
                    endpoint,
                    tenantInfo);

                _projectClient = new AIProjectClient(endpointUri, credential);
                _openAIClient = _projectClient.ProjectOpenAIClient;
            }
            else
            {
                var apiKey = defaultSettings.ApiKey
                    ?? throw new InvalidOperationException("API key is required when using ApiKey authentication.");

                _logger.LogInformation(
                    "Creating ProjectOpenAIClient with API key authentication for {Endpoint}.",
                    endpoint);

                var apiKeyCred = new ApiKeyCredential(apiKey);
                var authPolicy = ApiKeyAuthenticationPolicy.CreateBearerAuthorizationPolicy(apiKeyCred);
                _openAIClient = new ProjectOpenAIClient(
                    authPolicy,
                    new ProjectOpenAIClientOptions { Endpoint = endpointUri });
            }

            return _openAIClient;
        }
    }
}
