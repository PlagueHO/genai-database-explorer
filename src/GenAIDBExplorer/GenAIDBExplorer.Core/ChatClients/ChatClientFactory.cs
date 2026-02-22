using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace GenAIDBExplorer.Core.ChatClients;

/// <summary>
/// Factory for creating AI chat clients and embedding generators from project configuration.
/// Uses <see cref="AzureOpenAIClient"/> for Azure OpenAI service, supporting both
/// Entra ID and API key authentication.
/// </summary>
public sealed class ChatClientFactory(
    IProject project,
    ILogger<ChatClientFactory> logger
) : IChatClientFactory
{
    private readonly IProject _project = project;
    private readonly ILogger<ChatClientFactory> _logger = logger;

    /// <inheritdoc />
    public IChatClient CreateChatClient()
    {
        var defaultSettings = _project.Settings.OpenAIService.Default;
        var chatSettings = _project.Settings.OpenAIService.ChatCompletion;

        var deploymentId = chatSettings.AzureOpenAIDeploymentId
            ?? throw new InvalidOperationException("AzureOpenAI ChatCompletion deployment ID is required.");

        _logger.LogDebug("Creating chat client with deployment: {DeploymentId}", deploymentId);

        var azureClient = CreateAzureOpenAIClient(defaultSettings);
        return azureClient.GetChatClient(deploymentId).AsIChatClient();
    }

    /// <inheritdoc />
    public IChatClient CreateStructuredOutputChatClient()
    {
        var defaultSettings = _project.Settings.OpenAIService.Default;
        var chatSettings = _project.Settings.OpenAIService.ChatCompletionStructured;

        var deploymentId = chatSettings.AzureOpenAIDeploymentId
            ?? throw new InvalidOperationException("AzureOpenAI ChatCompletionStructured deployment ID is required.");

        _logger.LogDebug("Creating structured output chat client with deployment: {DeploymentId}", deploymentId);

        var azureClient = CreateAzureOpenAIClient(defaultSettings);
        return azureClient.GetChatClient(deploymentId).AsIChatClient();
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator()
    {
        var defaultSettings = _project.Settings.OpenAIService.Default;
        var embeddingSettings = _project.Settings.OpenAIService.Embedding;

        var deploymentId = embeddingSettings.AzureOpenAIDeploymentId
            ?? throw new InvalidOperationException("AzureOpenAI Embedding deployment ID is required.");

        _logger.LogDebug("Creating embedding generator with deployment: {DeploymentId}", deploymentId);

        var azureClient = CreateAzureOpenAIClient(defaultSettings);
        return azureClient.GetEmbeddingClient(deploymentId).AsIEmbeddingGenerator();
    }

    private AzureOpenAIClient CreateAzureOpenAIClient(OpenAIServiceDefaultSettings defaultSettings)
    {
        var endpoint = defaultSettings.AzureOpenAIEndpoint
            ?? throw new InvalidOperationException("AzureOpenAI endpoint is required.");

        var endpointUri = new Uri(endpoint);

        if (defaultSettings.AzureAuthenticationType == AzureOpenAIAuthenticationType.EntraIdAuthentication)
        {
            var credential = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = defaultSettings.TenantId })
                : new DefaultAzureCredential();

            var tenantInfo = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                ? $" for tenant {defaultSettings.TenantId}"
                : string.Empty;
            _logger.LogInformation(
                "Using Microsoft Entra ID Default authentication for Azure OpenAI{TenantInfo}.",
                tenantInfo);

            return new AzureOpenAIClient(endpointUri, credential);
        }
        else
        {
            var apiKey = defaultSettings.AzureOpenAIKey
                ?? throw new InvalidOperationException("AzureOpenAI API key is required when using ApiKey authentication.");

            _logger.LogInformation("Using API key authentication for Azure OpenAI.");

            return new AzureOpenAIClient(endpointUri, new AzureKeyCredential(apiKey));
        }
    }
}
