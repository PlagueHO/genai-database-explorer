using System.CommandLine;
using System;
using System.IO;
using System.CommandLine.Invocation;

namespace GenAIDBExplorer.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("GenAI Database Explorer tool");

        var projectOption = new Option<FileInfo>(
            aliases: [ "--project", "-p" ],
            description: "The path to the GenAI Database Explorer project."
        )
        {
            IsRequired = true
        };

        rootCommand.AddGlobalOption(projectOption);

        var commands = new Dictionary<string, Action<FileInfo>>
        {
            { "init", InitializeProject },
            { "build", BuildProject },
            { "query", QueryProject }
        };

        foreach (var kvp in commands)
        {
            var command = new Command(kvp.Key, $"{kvp.Key} a GenAI Database Explorer project.");
            command.AddOption(projectOption);
            command.SetHandler(kvp.Value, projectOption);
            rootCommand.Add(command);
        }

        rootCommand.SetHandler(() =>
        {
            System.Console.WriteLine("Command not specified.");
        });

        return await rootCommand.InvokeAsync(args);
    }

    public static void InitializeProject(FileInfo projectPath)
    {
        System.Console.WriteLine($"Initializing project at '{projectPath.FullName}'.");
    }

    public static void BuildProject(FileInfo projectPath)
    {
        System.Console.WriteLine($"Building project at '{projectPath.FullName}'.");
    }

    public static void QueryProject(FileInfo projectPath)
    {
        System.Console.WriteLine($"Querying project at '{projectPath.FullName}'.");
    }
}
