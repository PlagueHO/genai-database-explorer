using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

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
        kernelBuilder.Services.AddLogging(
            c => c.AddSimpleConsole(
                options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                }));

        _logger.LogDebug("Adding ChatCompletion service with endpoint: {Endpoint}, service type: {ServiceType}", 
            _project.Settings.OpenAIService.Default.AzureOpenAIEndpoint, 
            _project.Settings.OpenAIService.Default.ServiceType);
        AddChatCompletionService(kernelBuilder, _project.Settings.OpenAIService.Default, _project.Settings.OpenAIService.ChatCompletion, "ChatCompletion");
        
        _logger.LogDebug("Adding ChatCompletionStructured service with endpoint: {Endpoint}, service type: {ServiceType}", 
            _project.Settings.OpenAIService.Default.AzureOpenAIEndpoint, 
            _project.Settings.OpenAIService.Default.ServiceType);
        AddChatCompletionService(kernelBuilder, _project.Settings.OpenAIService.Default, _project.Settings.OpenAIService.ChatCompletionStructured, "ChatCompletionStructured");

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
            var apiKey = defaultSettings.AzureOpenAIKey ?? throw new InvalidOperationException("AzureOpenAI API key is required");
            
            _logger.LogDebug("Adding Azure OpenAI chat completion service - ServiceId: {ServiceId}, DeploymentId: {DeploymentId}, Endpoint: {Endpoint}", 
                serviceId, deploymentId, endpoint);
            
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentId,
                endpoint: endpoint,
                apiKey: apiKey,
                serviceId: serviceId
            );
            
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
}
