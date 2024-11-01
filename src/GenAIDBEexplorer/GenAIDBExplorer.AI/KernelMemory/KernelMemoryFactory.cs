using GenAIDBExplorer.Models.Project;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.AzureOpenAI;
using Microsoft.KernelMemory.AI.OpenAI;
using System;

namespace GenAIDBExplorer.AI.KernelMemory
{
    public class KernelMemoryFactory : IKernelMemoryFactory
    {
        /// <summary>
        /// Factory method for <see cref="IServiceCollection"/>
        /// </summary>
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public Func<IServiceProvider, IKernelMemory> CreateKernelMemory(IProject project)
        {
            return CreateKernel;

            IKernelMemory CreateKernel(IServiceProvider provider)
            {
                var kernelMemoryBuilder = new KernelMemoryBuilder();

                var loggerFactory = provider.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    kernelMemoryBuilder.Services.AddSingleton(loggerFactory);
                }

                var apikey = project.EmbeddingSettings.AzureOpenAIKey;

                if (!string.IsNullOrWhiteSpace(apikey))
                {
                    var endpoint = project.EmbeddingSettings.AzureOpenAIEndpoint ??
                                   throw new InvalidDataException($"No endpoint configured in {nameof(project.EmbeddingSettings.AzureOpenAIEndpoint)}.");

                    var modelEmbedding =
                        project.EmbeddingSettings.AzureOpenAIDeploymentId ??
                        DefaultEmbedModel;

                    kernelMemoryBuilder.WithTextEmbeddingGeneration(new AzureOpenAITextEmbeddingGeneration(modelEmbedding, modelEmbedding, endpoint, apikey));

                    return kernelMemoryBuilder.Build();
                }

                apikey = project.EmbeddingSettings.OpenAIKey;

                if (!string.IsNullOrWhiteSpace(apikey))
                {
                    var modelEmbedding =
                        project.EmbeddingSettings.AzureOpenAIDeploymentId ??
                        DefaultEmbedModel;

                    kernelMemoryBuilder.WithTextEmbeddingGeneration(new OpenAITextEmbeddingGeneration(modelEmbedding, apikey));

                    return kernelMemoryBuilder.Build();
                }

                throw new InvalidDataException($"No api-key configured in {nameof(project.EmbeddingSettings.AzureOpenAIKey)} or {nameof(project.EmbeddingSettings.OpenAIKey)}.");
            }
        }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
}