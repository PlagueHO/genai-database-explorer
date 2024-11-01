using System.CommandLine;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Data.DatabaseProviders;
using GenAIDBExplorer.AI.SemanticKernel;
using GenAIDBExplorer.AI.KernelMemory;

namespace GenAIDBExplorer.Console;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        using var traceListener = new TextWriterTraceListener("genaidbexplorer.log");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(
                (context, config) =>
                {
                    config
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                        .AddEnvironmentVariables()
                        .AddUserSecrets(Assembly.GetExecutingAssembly());
                })
            .ConfigureLogging(
                (context, config) =>
                {
                    config
                        .ClearProviders()
                        .AddConfiguration(context.Configuration.GetSection("Logging"));
                })
            .ConfigureServices(
                (context, services) =>
                {
                    services
                        .AddSingleton(SemanticKernelFactory.CreateSemanticKernel(context.Configuration))
                        .AddSingleton(KernelMemoryFactory.CreateKernelMemory(context.Configuration))
                        .AddSingleton(SqlConnectionProvider.Create(context.Configuration))
                        .AddSingleton<CommandLineRunner>();
                })
            .UseConsoleLifetime()
            .Build();

        await host.Services.GetRequiredService<CommandLineRunner>().RunAsync(args);
    }
}
