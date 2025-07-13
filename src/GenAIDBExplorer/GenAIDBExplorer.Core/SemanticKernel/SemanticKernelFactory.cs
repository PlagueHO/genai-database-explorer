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

        AddChatCompletionService(kernelBuilder, _project.Settings.OpenAIService.Default, _project.Settings.OpenAIService.ChatCompletion, "ChatCompletion");
        AddChatCompletionService(kernelBuilder, _project.Settings.OpenAIService.Default, _project.Settings.OpenAIService.ChatCompletionStructured, "ChatCompletionStructured");

        return kernelBuilder.Build();
    }

    /// <summary>
    /// Adds the appropriate chat completion service to the kernel builder based on the settings.
    /// </summary>
    /// <param name="kernelBuilder">The kernel builder to configure.</param>
    /// <param name="defaultSettings">The default OpenAI service settings.</param>
    /// <param name="chatCompletionSettings">The chat completion specific settings.</param>
    /// <param name="serviceId">The unique service identifier.</param>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
    private static void AddChatCompletionService(
            IKernelBuilder kernelBuilder,
            OpenAIServiceDefaultSettings defaultSettings,
            IOpenAIServiceChatCompletionSettings chatCompletionSettings,
            string serviceId)
    {
        if (defaultSettings.ServiceType == "AzureOpenAI")
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: chatCompletionSettings.AzureOpenAIDeploymentId ?? throw new InvalidOperationException("AzureOpenAI deployment ID is required"),
                endpoint: defaultSettings.AzureOpenAIEndpoint ?? throw new InvalidOperationException("AzureOpenAI endpoint is required"),
                apiKey: defaultSettings.AzureOpenAIKey ?? throw new InvalidOperationException("AzureOpenAI API key is required"),
                serviceId: serviceId
            );
        }
        else
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: chatCompletionSettings.ModelId ?? throw new InvalidOperationException("OpenAI model ID is required"),
                apiKey: defaultSettings.OpenAIKey ?? throw new InvalidOperationException("OpenAI API key is required"),
                serviceId: serviceId
            );
        }
    }
}
