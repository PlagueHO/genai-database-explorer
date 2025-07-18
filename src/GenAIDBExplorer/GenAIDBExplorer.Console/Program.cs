using System.CommandLine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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
    /// <returns>The exit code.</returns>
    private static async Task<int> Main(string[] args)
    {
        // Create the root command with a description
        var rootCommand = new RootCommand("GenAI Database Explorer console application");

        // Build the host
        var builder = Host.CreateApplicationBuilder(args);
        builder.ConfigureHost(args);
        var host = builder.Build();

        // Set up commands
        rootCommand.Subcommands.Add(InitProjectCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(DataDictionaryCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(EnrichModelCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(ExportModelCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(ExtractModelCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(QueryModelCommandHandler.SetupCommand(host));
        rootCommand.Subcommands.Add(ShowObjectCommandHandler.SetupCommand(host));

        try
        {
            await rootCommand.Parse(args).InvokeAsync();
            return 0;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
