using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace GenAIDBExplorer.Console;

public sealed class CommandLineRunner
{
    public async Task<int> RunAsync(string[] args)
    {
        var rootCommand = new RootCommand("GenAI Database Explorer tool");

        var projectOption = new Option<DirectoryInfo>(
            aliases: new[] { "--project", "-p" },
            description: "The path to the GenAI Database Explorer project."
        )
        {
            IsRequired = true
        };

        rootCommand.AddGlobalOption(projectOption);

        var commands = new Dictionary<string, Action<DirectoryInfo>>
        {
            { "init", CommandLineRunner.InitializeProject },
            { "build", CommandLineRunner.BuildProject },
            { "query", CommandLineRunner.QueryProject }
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

    private static void ValidateProjectPath(DirectoryInfo projectPath)
    {
        if (projectPath == null)
        {
            throw new ArgumentNullException(nameof(projectPath), "Project path cannot be null.");
        }
    }

    public static void InitializeProject(DirectoryInfo projectPath)
    {
        ValidateProjectPath(projectPath);
        System.Console.WriteLine($"Initializing project at '{projectPath.FullName}'.");

        // Create the project directory if it doesn't exist
        if (!projectPath.Exists)
        {
            projectPath.Create();
        }
    }

    public static void BuildProject(DirectoryInfo projectPath)
    {
        ValidateProjectPath(projectPath);
        System.Console.WriteLine($"Building project at '{projectPath.FullName}'.");
    }

    public static void QueryProject(DirectoryInfo projectPath)
    {
        ValidateProjectPath(projectPath);
        System.Console.WriteLine($"Querying project at '{projectPath.FullName}'.");
    }
}