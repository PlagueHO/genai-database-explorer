using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;

namespace GenAIDBExplorer.AI.KernelMemory;

public static class KernelMemoryFactory
{
    /// <summary>
    /// Factory method for <see cref="IServiceCollection"/>
    /// </summary>
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public static Func<IServiceProvider, IKernelMemory> CreateKernelMemory(IConfiguration projectSettings)
    {
        return CreateKernelMemory;

        IKernelMemory CreateKernelMemory(IServiceProvider provider)
        {
            var kernelMemoryBuilder = new KernelMemoryBuilder();

            var loggerFactory = provider.GetService<ILoggerFactory>();
            if (loggerFactory != null)
            {
                kernelMemoryBuilder.Services.AddSingleton(loggerFactory);
            }

            var apikey = projectSettings.GetValue<string>(SettingNameAzureApiKey);

            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var endpoint = projectSettings.GetValue<string>(SettingNameAzureEndpoint) ??
                               throw new InvalidDataException($"No endpoint configured in {SettingNameAzureEndpoint}.");

                var modelEmbedding =
                    projectSettings.GetValue<string>(SettingNameAzureModelEmbedding) ??
                    DefaultEmbedModel;

                kernelMemoryBuilder.WithTextEmbeddingGeneration(new AzureOpenAITextEmbeddingGeneration(modelEmbedding, modelEmbedding, endpoint, apikey));

                return kernelMemoryBuilder.Build();
            }

            apikey = projectSettings.GetValue<string>(SettingNameOpenAIApiKey);

            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var modelEmbedding =
                    projectSettings.GetValue<string>(SettingNameOpenAIModelEmbedding) ??
                    DefaultEmbedModel;

                kernelMemoryBuilder.WithTextEmbeddingGeneration(new OpenAITextEmbeddingGeneration(modelEmbedding, apikey));

                return kernelMemoryBuilder.Build();
            }

            throw new InvalidDataException($"No api-key configured in {SettingNameAzureApiKey} or {SettingNameOpenAIApiKey}.");
        }
    }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
