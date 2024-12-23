using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace GenAIDBExplorer.Core.SemanticKernel;

public class SemanticKernelFactory(
    IProject project,
    ILogger<SemanticKernelFactory> logger
) : ISemanticKernelFactory
{
    private readonly IProject _project = project;
    private readonly ILogger<SemanticKernelFactory> _logger = logger;

    /// <summary>
    /// Factory method for <see cref="IServiceCollection"/>
    /// </summary>
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
    /// <param name="kernelBuilder">The kernel builder.</param>
    /// <param name="settings">The chat completion settings.</param>
    /// <param name="serviceId">The service ID.</param>
    private static void AddChatCompletionService(
            IKernelBuilder kernelBuilder,
            OpenAIServiceDefaultSettings openAIServiceDefaultSettings,
            IOpenAIServiceChatCompletionSettings openAIServiceChatCompletionSettings,
            string serviceId)
    {
        if (openAIServiceDefaultSettings.ServiceType == "AzureOpenAI")
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: openAIServiceChatCompletionSettings.AzureOpenAIDeploymentId,
                endpoint: openAIServiceDefaultSettings.AzureOpenAIEndpoint,
                apiKey: openAIServiceDefaultSettings.AzureOpenAIKey,
                serviceId: serviceId
            );
        }
        else
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: openAIServiceChatCompletionSettings.ModelId,
                apiKey: openAIServiceDefaultSettings.OpenAIKey,
                serviceId: serviceId
            );
        }
    }
}
