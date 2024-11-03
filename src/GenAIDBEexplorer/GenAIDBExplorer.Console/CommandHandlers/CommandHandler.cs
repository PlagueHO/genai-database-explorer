using GenAIDBExplorer.Models.Project;
using Microsoft.Extensions.Logging;
using System;

namespace GenAIDBExplorer.Console.CommandHandlers
{
    /// <summary>
    /// Abstract base class for command handlers.
    /// </summary>
    /// <remarks>
    /// This class provides common utility functionality for handling console commands.
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CommandHandler"/> class.
    /// </remarks>
    public abstract class CommandHandler(IProjectFactory projectFactory, IServiceProvider serviceProvider, ILogger<ICommandHandler> logger) : ICommandHandler
    {
        /// <summary>
        /// The project factory instance for creating project instances.
        /// </summary>
        protected readonly IProjectFactory _projectFactory = projectFactory ?? throw new ArgumentNullException(nameof(projectFactory));

        /// <summary>
        /// Service provider instance for resolving dependencies.
        /// </summary>
        protected readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        /// <summary>
        /// Logger instance for logging information, warnings, and errors.
        /// </summary>
        protected readonly ILogger<ICommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Handles the command with the specified project path.
        /// </summary>
        /// <param name="projectPath">The directory path of the project to handle.</param>
        public abstract void Handle(DirectoryInfo projectPath);

        /// <summary>
        /// Validates the specified project path.
        /// </summary>
        /// <param name="projectPath">The project path to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when the project path is null.</exception>
        protected static void ValidateProjectPath(DirectoryInfo projectPath)
        {
            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath), "Project path cannot be null.");
            }
        }

        /// <summary>
        /// Outputs an informational message to the console.
        /// </summary>
        /// <param name="message">The message to output.</param>
        protected static void OutputInformation(string message)
        {
            System.Console.WriteLine(message);
        }

        /// <summary>
        /// Outputs a warning message to the console in yellow text.
        /// </summary>
        /// <param name="message">The message to output.</param>
        protected static void OutputWarning(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(message);
            System.Console.ResetColor();
        }

        /// <summary>
        /// Output a stop error to the console as red text.
        /// </summary>
        /// <param name="message">The message to output.</param>
        protected static void OutputStopError(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine(message);
            System.Console.ResetColor();
        }
    }
}