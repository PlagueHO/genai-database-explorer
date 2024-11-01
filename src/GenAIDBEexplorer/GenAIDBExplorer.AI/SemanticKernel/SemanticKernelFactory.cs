using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.AI.SemanticKernel;

public class SemanticKernelFactory : ISemanticKernelFactory
{
    /// <summary>
    /// Factory method for <see cref="IServiceCollection"/>
    /// </summary>
    public Func<IServiceProvider, Kernel> CreateSemanticKernel(IProject project)
    {
        return CreateKernel;

        Kernel CreateKernel(IServiceProvider provider)
        {
            var builder = new KernelBuilder();

            builder.Services.AddLogging();

            var apikey = project.ChatCompletionSettings.AzureOpenAIKey;

            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var endpoint = project.ChatCompletionSettings.AzureOpenAIEndpoint ??
                               throw new InvalidDataException($"No endpoint configured in {nameof(project.ChatCompletionSettings.AzureOpenAIEndpoint)}.");

                var modelCompletion =
                    project.ChatCompletionSettings.AzureOpenAIDeploymentId ??
                    DefaultChatModel;

                builder.AddAzureOpenAIChatCompletion(modelCompletion, modelCompletion, endpoint, apikey);

                return (Kernel)builder.Build();
            }

            apikey = project.ChatCompletionSettings.OpenAIKey;

            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var modelCompletion =
                    project.ChatCompletionSettings.AzureOpenAIDeploymentId ??
                    DefaultChatModel;

                builder.AddOpenAIChatCompletion(modelCompletion, apikey);

                return (Kernel)builder.Build();
            }

            throw new InvalidDataException($"No api-key configured in {nameof(project.ChatCompletionSettings.AzureOpenAIKey)} or {nameof(project.ChatCompletionSettings.OpenAIKey)}.");
        }
    }
}