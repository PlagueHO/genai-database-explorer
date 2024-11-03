using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for building a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BuildCommandHandler"/> class.
/// </remarks>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
public class BuildCommandHandler(ILogger<ICommandHandler> logger, IServiceProvider serviceProvider) : CommandHandler(logger, serviceProvider)
{
    /// <summary>
    /// Handles the build command with the specified project path.
    /// </summary>
    /// <param name="projectPath">The directory path of the project to build.</param>
    public override void Handle(DirectoryInfo projectPath)
    {
        _logger.LogInformation($"Building project at '{projectPath.FullName}'.");

        // Get the IProject service instance
        var project = _serviceProvider.GetRequiredService<IProject>();

    }
}