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

        if (_project.Settings.ChatCompletion.ServiceType == "AzureOpenAI")
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: _project.Settings.ChatCompletion.AzureOpenAIDeploymentId,
                endpoint: _project.Settings.ChatCompletion.AzureOpenAIEndpoint,
                apiKey: _project.Settings.ChatCompletion.AzureOpenAIKey
            );
        }
        else
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: _project.Settings.ChatCompletion.Model,
                apiKey: _project.Settings.ChatCompletion.OpenAIKey
            );
        }

        kernelBuilder.Services.AddLogging();

        return kernelBuilder.Build();
    }
}
