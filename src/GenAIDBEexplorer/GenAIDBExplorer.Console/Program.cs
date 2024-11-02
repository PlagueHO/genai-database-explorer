using System.CommandLine;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.AI.SemanticKernel;
using GenAIDBExplorer.AI.KernelMemory;
using GenAIDBExplorer.Models.Project;
using System.IO;
using System.CommandLine.Invocation;

namespace GenAIDBExplorer.Console;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    private static async Task Main(string[] args)
    {
        using var traceListener = new TextWriterTraceListener("genaidbexplorer.log");

        var rootCommand = new RootCommand("GenAI Database Explorer tool");

        var projectOption = new Option<DirectoryInfo>(
            aliases: new[] { "--project", "-p" },
            description: "The path to the GenAI Database Explorer project."
        )
        {
            IsRequired = true
        };

        rootCommand.AddGlobalOption(projectOption);

        rootCommand.SetHandler(async (DirectoryInfo project) =>
        {
            var host = CreateHostBuilder(args, project.FullName).Build();
            var runner = host.Services.GetRequiredService<CommandLineRunner>();
            await runner.RunAsync(args);
        }, projectOption);

        await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Creates and configures an <see cref="IHostBuilder"/> instance.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="projectFolderPath">The path to the project folder.</param>
    /// <returns>An <see cref="IHostBuilder"/> instance.</returns>
    static IHostBuilder CreateHostBuilder(string[] args, string projectFolderPath) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                if (!Directory.Exists(projectFolderPath))
                {
                    throw new DirectoryNotFoundException($"The specified project folder path '{projectFolderPath}' does not exist.");
                }

                config
                    .AddJsonFile("appsettings.json")
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
                // Register IConfiguration for the handling the project settings
                services.AddSingleton<IConfiguration>(provider =>
                {
                    var configurationBuilder = new ConfigurationBuilder()
                        .SetBasePath(projectFolderPath)
                        .AddJsonFile("settings.json", optional: false, reloadOnChange: false);

                    return configurationBuilder.Build();
                });

                // Register Project class as IProject
                services.AddSingleton<IProject, Project>();

                // Register Semantic Kernel Factory
                services.AddSingleton(provider =>
                {
                    var project = provider.GetRequiredService<IProject>();
                    return new SemanticKernelFactory().CreateSemanticKernel(project)(provider);
                });

                // Register Kernel Memory Factory
                services.AddSingleton(provider =>
                {
                    var project = provider.GetRequiredService<IProject>();
                    return new KernelMemoryFactory().CreateKernelMemory(project)(provider);
                });

                // Register the SQL connection provider directly
                services.AddSingleton<IDatabaseConnectionProvider, SqlConnectionProvider>();

                // Register the command-line runner
                services.AddSingleton<CommandLineRunner>();
            })
            .UseConsoleLifetime();
}
