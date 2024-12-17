using System.CommandLine;
using Microsoft.Extensions.Hosting;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Console.Extensions;

namespace GenAIDBExplorer.Console;

/// <summary>
/// The main entry point for the GenAI Database Explorer tool.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main method that sets up and runs the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    private static async Task Main(string[] args)
    {
        // Create the root command with a description
        var rootCommand = new RootCommand("GenAI Database Explorer console application");

        // Build the host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureHost(args)
            .Build();

        // Set up commands
        rootCommand.AddCommand(InitProjectCommandHandler.SetupCommand(host));
        rootCommand.AddCommand(DataDictionaryCommandHandler.SetupCommand(host));
        rootCommand.AddCommand(EnrichModelCommandHandler.SetupCommand(host));
        rootCommand.AddCommand(ExportModelCommandHandler.SetupCommand(host));
        rootCommand.AddCommand(ExtractModelCommandHandler.SetupCommand(host));
        rootCommand.AddCommand(QueryModelCommandHandler.SetupCommand(host));
        rootCommand.AddCommand(ShowObjectCommandHandler.SetupCommand(host));

        // Invoke the root command
        await rootCommand.InvokeAsync(args);
    }
}
