using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.AI.SemanticKernel;
using GenAIDBExplorer.AI.KernelMemory;
using GenAIDBExplorer.Models.Project;
using GenAIDBExplorer.Console.CommandHandlers;

namespace GenAIDBExplorer.Console.Extensions;

/// <summary>
/// Extension methods for configuring the host builder.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the host builder with the necessary services and configurations.
    /// </summary>
    /// <param name="builder">The <see cref="IHostBuilder"/> instance.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The configured <see cref="IHostBuilder"/> instance.</returns>
    public static IHostBuilder ConfigureHost(this IHostBuilder builder, string[] args)
    {
        return builder
            .ConfigureAppConfiguration((context, config) =>
            {
                config
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureLogging((context, config) =>
            {
                config
                    .ClearProviders()
                    .AddConfiguration(context.Configuration.GetSection("Logging"))
                    .AddConsole();
            })
            .ConfigureServices((context, services) =>
            {
                // Register command handlers
                services.AddSingleton<InitCommandHandler>();
                services.AddSingleton<BuildCommandHandler>();
                services.AddSingleton<QueryCommandHandler>();

                // Register the project service
                services.AddSingleton<IProject, Project>();

                // Register the database connection provider
                services.AddSingleton<IDatabaseConnectionProvider, SqlConnectionProvider>();

                // Register the Semantic Kernel factory
                services.AddSingleton(provider =>
                {
                    var project = provider.GetRequiredService<IProject>();
                    return new SemanticKernelFactory().CreateSemanticKernel(project)(provider);
                });

                // Register the Kernel Memory factory
                services.AddSingleton(provider =>
                {
                    var project = provider.GetRequiredService<IProject>();
                    return new KernelMemoryFactory().CreateKernelMemory(project)(provider);
                });
            })
            .UseConsoleLifetime();
    }
}
