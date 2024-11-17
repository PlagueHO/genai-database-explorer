﻿using GenAIDBExplorer.Core.Models.Project;
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
        kernelBuilder.Services.AddLogging();

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
        
        return kernelBuilder.Build();
    }
}