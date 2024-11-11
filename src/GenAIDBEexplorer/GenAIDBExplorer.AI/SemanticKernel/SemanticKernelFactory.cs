using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using GenAIDBExplorer.Models.Project;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.AI.SemanticKernel;

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

        if (project.Settings.ChatCompletion.ServiceType == "AzureOpenAI")
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: project.Settings.ChatCompletion.AzureOpenAIDeploymentId,
                endpoint: project.Settings.ChatCompletion.AzureOpenAIEndpoint,
                apiKey: project.Settings.ChatCompletion.AzureOpenAIKey
            );
        }
        else
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: project.Settings.ChatCompletion.Model,
                apiKey: project.Settings.ChatCompletion.OpenAIKey
            );
        }

        kernelBuilder.Services.AddLogging();

        return kernelBuilder.Build();
    }
}
