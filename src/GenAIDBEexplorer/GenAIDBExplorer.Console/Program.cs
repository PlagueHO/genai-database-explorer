using System.CommandLine;
using Microsoft.Extensions.Hosting;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Console.Extensions;
using Microsoft.Extensions.DependencyInjection;

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
        var rootCommand = new RootCommand("GenAI Database Explorer tool");

        // Define the project option
        var projectOption = new Option<DirectoryInfo>(
            aliases: new[] { "--project", "-p" },
            description: "The path to the GenAI Database Explorer project."
        )
        {
            IsRequired = true
        };

        // Add the project option as a global option
        rootCommand.AddGlobalOption(projectOption);

        // Build the host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureHost(args)
            .Build();

        // Define the init command
        var initCommand = new Command("init", "Initialize a GenAI Database Explorer project.");
        initCommand.AddOption(projectOption);
        initCommand.SetHandler((DirectoryInfo projectPath) =>
        {
            var handler = host.Services.GetRequiredService<InitCommandHandler>();
            handler.Handle(projectPath);
        }, projectOption);

        // Define the build command
        var buildCommand = new Command("build", "Build a GenAI Database Explorer project.");
        buildCommand.AddOption(projectOption);
        buildCommand.SetHandler((DirectoryInfo projectPath) =>
        {
            var handler = host.Services.GetRequiredService<BuildCommandHandler>();
            handler.Handle(projectPath);
        }, projectOption);

        // Define the query command
        var queryCommand = new Command("query", "Query a GenAI Database Explorer project.");
        queryCommand.AddOption(projectOption);
        queryCommand.SetHandler((DirectoryInfo projectPath) =>
        {
            var handler = host.Services.GetRequiredService<QueryCommandHandler>();
            handler.Handle(projectPath);
        }, projectOption);

        // Add commands to the root command
        rootCommand.AddCommand(initCommand);
        rootCommand.AddCommand(buildCommand);
        rootCommand.AddCommand(queryCommand);

        // Invoke the root command
        await rootCommand.InvokeAsync(args);
    }
}
