using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Azure.Identity;
using System.Net;

namespace GenAIDBExplorer.Core.SemanticKernel;

/// <summary>
/// Factory for creating configured Semantic Kernel instances.
/// </summary>
public class SemanticKernelFactory(
    IProject project,
    ILogger<SemanticKernelFactory> logger
) : ISemanticKernelFactory
{
    private readonly IProject _project = project;
    private readonly ILogger<SemanticKernelFactory> _logger = logger;

    /// <summary>
    /// Creates a configured Semantic Kernel instance with chat completion services.
    /// </summary>
    /// <returns>A configured <see cref="Kernel"/> instance.</returns>
    public Kernel CreateSemanticKernel()
    {
        _logger.LogDebug("Creating Semantic Kernel instance");

        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.Services.AddSingleton(_logger);

        // Configure HTTP client retry policy for rate limiting (429) and transient errors
        kernelBuilder.Services.ConfigureHttpClientDefaults(c =>
        {
            c.AddStandardResilienceHandler().Configure(o =>
            {
                o.Retry.MaxRetryAttempts = 10;
                o.Retry.ShouldHandle = args =>
                    ValueTask.FromResult(args.Outcome.Result?.StatusCode is HttpStatusCode.TooManyRequests or
                        (>= HttpStatusCode.InternalServerError and <= (HttpStatusCode)599));
                o.Retry.OnRetry = args =>
                {
                    _logger.LogWarning("HTTP request failed with status {StatusCode}. Retrying (attempt {AttemptNumber}).",
                        args.Outcome.Result?.StatusCode, args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                };
            });
        });

        _logger.LogDebug("Adding ChatCompletion service with endpoint: {Endpoint}, service type: {ServiceType}",
            _project.Settings.OpenAIService.Default.AzureOpenAIEndpoint,
            _project.Settings.OpenAIService.Default.ServiceType);
        AddChatCompletionService(kernelBuilder, _project.Settings.OpenAIService.Default, _project.Settings.OpenAIService.ChatCompletion, "ChatCompletion");

        _logger.LogDebug("Adding ChatCompletionStructured service with endpoint: {Endpoint}, service type: {ServiceType}",
            _project.Settings.OpenAIService.Default.AzureOpenAIEndpoint,
            _project.Settings.OpenAIService.Default.ServiceType);
        AddChatCompletionService(kernelBuilder, _project.Settings.OpenAIService.Default, _project.Settings.OpenAIService.ChatCompletionStructured, "ChatCompletionStructured");

        // Add Embedding service (obsolete SK API fallback). ServiceId should match VectorIndexOptions default "Embeddings".
        AddEmbeddingService(kernelBuilder, _project.Settings.OpenAIService.Default, _project.Settings.OpenAIService.Embedding, serviceId: "Embeddings");

        var kernel = kernelBuilder.Build();
        _logger.LogDebug("Semantic Kernel instance created successfully");

        return kernel;
    }

    /// <summary>
    /// Adds the appropriate chat completion service to the kernel builder based on the settings.
    /// </summary>
    /// <param name="kernelBuilder">The kernel builder to configure.</param>
    /// <param name="defaultSettings">The default OpenAI service settings.</param>
    /// <param name="chatCompletionSettings">The chat completion specific settings.</param>
    /// <param name="serviceId">The unique service identifier.</param>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
    private void AddChatCompletionService(
        IKernelBuilder kernelBuilder,
        OpenAIServiceDefaultSettings defaultSettings,
        IOpenAIServiceChatCompletionSettings chatCompletionSettings,
        string serviceId)
    {
        if (defaultSettings.ServiceType == "AzureOpenAI")
        {
            var deploymentId = chatCompletionSettings.AzureOpenAIDeploymentId ?? throw new InvalidOperationException("AzureOpenAI deployment ID is required");
            var endpoint = defaultSettings.AzureOpenAIEndpoint ?? throw new InvalidOperationException("AzureOpenAI endpoint is required");

            _logger.LogDebug("Adding Azure OpenAI chat completion service - ServiceId: {ServiceId}, DeploymentId: {DeploymentId}, Endpoint: {Endpoint}",
                serviceId, deploymentId, endpoint);

            if (defaultSettings.AzureAuthenticationType == AzureOpenAIAuthenticationType.EntraIdAuthentication)
            {
                // Use DefaultAzureCredential for Entra ID authentication
                var credential = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                    ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = defaultSettings.TenantId })
                    : new DefaultAzureCredential();

                var tenantInfo = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                    ? $" for tenant {defaultSettings.TenantId}"
                    : string.Empty;
                _logger.LogInformation("Using Microsoft Entra ID Default authentication for Azure OpenAI{TenantInfo} (supports managed identity, Visual Studio, Azure CLI, etc.).", tenantInfo);

                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: deploymentId,
                    endpoint: endpoint,
                    credential,
                    serviceId: serviceId
                );
            }
            else
            {
                // Use API key authentication
                var apiKey = defaultSettings.AzureOpenAIKey ?? throw new InvalidOperationException("AzureOpenAI API key is required when using ApiKey authentication");
                _logger.LogInformation("Using API key authentication for Azure OpenAI.");

                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: deploymentId,
                    endpoint: endpoint,
                    apiKey: apiKey,
                    serviceId: serviceId
                );
            }

            _logger.LogDebug("Successfully added Azure OpenAI chat completion service: {ServiceId}", serviceId);
        }
        else
        {
            var modelId = chatCompletionSettings.ModelId ?? throw new InvalidOperationException("OpenAI model ID is required");
            var apiKey = defaultSettings.OpenAIKey ?? throw new InvalidOperationException("OpenAI API key is required");

            _logger.LogDebug("Adding OpenAI chat completion service - ServiceId: {ServiceId}, ModelId: {ModelId}",
                serviceId, modelId);

            kernelBuilder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey,
                serviceId: serviceId
            );

            _logger.LogDebug("Successfully added OpenAI chat completion service: {ServiceId}", serviceId);
        }
    }

    /// <summary>
    /// Adds a text embedding generation service using SK's embedding generation registration (obsolete path) so that
    /// it can be adapted to Microsoft.Extensions.AI IEmbeddingGenerator at runtime.
    /// </summary>
    // Suppress SK experimental diagnostics for embedding generator registration
#pragma warning disable SKEXP0010 // Evaluation-only APIs subject to change
    private void AddEmbeddingService(
        IKernelBuilder kernelBuilder,
        OpenAIServiceDefaultSettings defaultSettings,
        IOpenAIServiceEmbeddingSettings embeddingSettings,
        string serviceId)
    {
        if (defaultSettings.ServiceType == "AzureOpenAI")
        {
            var deploymentId = embeddingSettings.AzureOpenAIDeploymentId;
            var endpoint = defaultSettings.AzureOpenAIEndpoint;

            if (string.IsNullOrWhiteSpace(deploymentId) || string.IsNullOrWhiteSpace(endpoint))
            {
                _logger.LogWarning("Skipping Azure OpenAI embedding registration due to missing configuration. DeploymentId present: {HasDeployment}, Endpoint present: {HasEndpoint}",
                    !string.IsNullOrWhiteSpace(deploymentId), !string.IsNullOrWhiteSpace(endpoint));
                return; // Do not throw during tests or when embeddings are not configured
            }

            if (defaultSettings.AzureAuthenticationType == AzureOpenAIAuthenticationType.EntraIdAuthentication)
            {
                // Use DefaultAzureCredential for Entra ID authentication
                var credential = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                    ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = defaultSettings.TenantId })
                    : new DefaultAzureCredential();

                var tenantInfo = !string.IsNullOrWhiteSpace(defaultSettings.TenantId)
                    ? $" for tenant {defaultSettings.TenantId}"
                    : string.Empty;
                _logger.LogInformation("Using Microsoft Entra ID Default authentication for Azure OpenAI embeddings{TenantInfo} (supports managed identity, Visual Studio, Azure CLI, etc.).", tenantInfo);

                kernelBuilder.AddAzureOpenAIEmbeddingGenerator(
                    deploymentName: deploymentId,
                    endpoint: endpoint,
                    credential,
                    serviceId: serviceId);
            }
            else
            {
                // Use API key authentication
                var apiKey = defaultSettings.AzureOpenAIKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogWarning("Skipping Azure OpenAI embedding registration due to missing API key when using ApiKey authentication");
                    return;
                }

                _logger.LogInformation("Using API key authentication for Azure OpenAI embeddings.");

                kernelBuilder.AddAzureOpenAIEmbeddingGenerator(
                    deploymentName: deploymentId,
                    endpoint: endpoint,
                    apiKey: apiKey,
                    serviceId: serviceId);
            }
        }
        else
        {
            var modelId = embeddingSettings.ModelId;
            var apiKey = defaultSettings.OpenAIKey;

            if (string.IsNullOrWhiteSpace(modelId) || string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Skipping OpenAI embedding registration due to missing configuration. ModelId present: {HasModelId}",
                    !string.IsNullOrWhiteSpace(modelId));
                return; // Do not throw during tests or when embeddings are not configured
            }

            kernelBuilder.AddOpenAIEmbeddingGenerator(
                modelId: modelId,
                apiKey: apiKey,
                serviceId: serviceId);
        }
    }
#pragma warning restore SKEXP0010
}
