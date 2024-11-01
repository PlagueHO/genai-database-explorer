using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace GenAIDBExplorer.AI.SemanticKernel;

public static class SemanticKernelFactory
{
    /// <summary>
    /// Factory method for <see cref="IServiceCollection"/>
    /// </summary>
    public static Func<IServiceProvider, Kernel> CreateSemanticKernel(IConfiguration projectConfiguration)
    {
        return CreateKernel;

        Kernel CreateKernel(IServiceProvider provider)
        {
            var builder = new KernelBuilder();

            builder.Services.AddLogging();

            var apikey = projectConfiguration.GetValue<string>(SettingNameAzureApiKey);

            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var endpoint = configuration.GetValue<string>(SettingNameAzureEndpoint) ??
                               throw new InvalidDataException($"No endpoint configured in {SettingNameAzureEndpoint}.");

                var modelCompletion =
                    configuration.GetValue<string>(SettingNameAzureModelCompletion) ??
                    DefaultChatModel;

                builder.AddAzureOpenAIChatCompletion(modelCompletion, modelCompletion, endpoint, apikey);

                return (Kernel)builder.Build();
            }

            apikey = configuration.GetValue<string>(SettingNameOpenAIApiKey);

            if (!string.IsNullOrWhiteSpace(apikey))
            {
                var modelCompletion =
                    configuration.GetValue<string>(SettingNameOpenAIModelCompletion) ??
                    DefaultChatModel;

                builder.AddOpenAIChatCompletion(modelCompletion, apikey);

                return (Kernel)builder.Build();
            }

            throw new InvalidDataException($"No api-key configured in {SettingNameAzureApiKey} or {SettingNameOpenAIApiKey}.");
        }
    }
}
