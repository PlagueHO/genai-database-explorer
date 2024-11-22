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
        kernelBuilder.Services.AddLogging(c => c.AddConsole());

        AddChatCompletionService(kernelBuilder, _project.Settings.ChatCompletion, "ChatCompletion");
        AddChatCompletionService(kernelBuilder, _project.Settings.ChatCompletionStructured, "ChatCompletionStructured");

        return kernelBuilder.Build();
    }

    /// <summary>
    /// Adds the appropriate chat completion service to the kernel builder based on the settings.
    /// </summary>
    /// <param name="kernelBuilder">The kernel builder.</param>
    /// <param name="settings">The chat completion settings.</param>
    /// <param name="serviceId">The service ID.</param>
    private void AddChatCompletionService(IKernelBuilder kernelBuilder, IChatCompletionSettings settings, string serviceId)
    {
        if (settings.ServiceType == "AzureOpenAI")
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: settings.AzureOpenAIDeploymentId,
                endpoint: settings.AzureOpenAIEndpoint,
                apiKey: settings.AzureOpenAIKey,
                serviceId: serviceId
            );
        }
        else
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: settings.ModelId,
                apiKey: settings.OpenAIKey,
                serviceId: serviceId
            );
        }
    }
}
