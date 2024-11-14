﻿using System.CommandLine;
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
        var rootCommand = new RootCommand("GenAI Database Explorer tool");

        // Build the host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureHost(args)
            .Build();

        // Set up commands
        rootCommand.AddCommand(InitCommandHandler.SetupCommand(host));
        rootCommand.AddCommand(BuildCommandHandler.SetupCommand(host));
        rootCommand.AddCommand(QueryCommandHandler.SetupCommand(host));
        rootCommand.AddCommand(GenerateDescriptionCommandHandler.SetupCommand(host));

        // Invoke the root command
        await rootCommand.InvokeAsync(args);
    }
}
