using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for initializing a project.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InitCommandHandler"/> class.
/// </remarks>
/// <param name="project">The project instance to initialize.</param>
/// <param name="serviceProvider">The service provider instance for resolving dependencies.</param>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class InitCommandHandler(IProject project, IServiceProvider serviceProvider, ILogger<ICommandHandler> logger)
    : CommandHandler(project, serviceProvider, logger)
{
    /// <summary>
    /// Handles the initialization command with the specified project path.
    /// </summary>
    /// <param name="projectDirectory">The directory path of the project to initialize.</param>
    public override void Handle(DirectoryInfo projectDirectory)
    {
        _logger.LogInformation(LogMessages.InitializingProject, projectDirectory.FullName);

        ValidateProjectPath(projectDirectory);

        if (IsDirectoryNotEmpty(projectDirectory))
        {
            _logger.LogError(LogMessages.ProjectFolderNotEmpty);

            OutputStopError(LogMessages.ProjectFolderNotEmpty);
            return;
        }

        InitializeProjectDirectory(projectDirectory);

        _logger.LogInformation(LogMessages.ProjectInitialized, projectDirectory.FullName);
    }

    /// <summary>
    /// Initializes the project directory by copying the default project structure.
    /// </summary>
    /// <param name="projectDirectory">The directory path of the project to initialize.</param>
    private static void InitializeProjectDirectory(DirectoryInfo projectDirectory)
    {
        var defaultProjectDirectory = new DirectoryInfo("DefaultProject");
        CopyDirectory(defaultProjectDirectory, projectDirectory);
    }

    /// <summary>
    /// Checks if the specified directory is not empty.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <returns>True if the directory is not empty; otherwise, false.</returns>
    private static bool IsDirectoryNotEmpty(DirectoryInfo directory)
    {
        return directory.EnumerateFileSystemInfos().Any();
    }

    /// <summary>
    /// Copies the contents of one directory to another directory.
    /// </summary>
    /// <param name="sourceDirectory">The source directory to copy from.</param>
    /// <param name="destinationDirectory">The destination directory to copy to.</param>
    /// <remarks>
    /// This method copies all files and subdirectories from the source directory to the destination directory.
    /// </remarks>
    private static void CopyDirectory(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
    {
        if (!destinationDirectory.Exists)
        {
            destinationDirectory.Create();
        }

        CopyFiles(sourceDirectory, destinationDirectory);
        CopySubDirectories(sourceDirectory, destinationDirectory);
    }

    /// <summary>
    /// Copies files from the source directory to the destination directory.
    /// </summary>
    /// <param name="sourceDirectory">The source directory to copy from.</param>
    /// <param name="destinationDirectory">The destination directory to copy to.</param>
    private static void CopyFiles(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
    {
        foreach (var file in sourceDirectory.GetFiles())
        {
            var destinationFilePath = Path.Combine(destinationDirectory.FullName, file.Name);
            file.CopyTo(destinationFilePath, overwrite: true);
        }
    }

    /// <summary>
    /// Copies subdirectories from the source directory to the destination directory.
    /// </summary>
    /// <param name="sourceDirectory">The source directory to copy from.</param>
    /// <param name="destinationDirectory">The destination directory to copy to.</param>
    private static void CopySubDirectories(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
    {
        foreach (var subDirectory in sourceDirectory.GetDirectories())
        {
            var destinationSubDirectory = destinationDirectory.CreateSubdirectory(subDirectory.Name);
            CopyDirectory(subDirectory, destinationSubDirectory);
        }
    }
}

/// <summary>
/// Contains log messages used in the <see cref="InitCommandHandler"/> class.
/// </summary>
public static class LogMessages
{
    public const string InitializingProject = "Initializing project at '{ProjectPath}'";
    public const string ProjectFolderNotEmpty = "The project folder is not empty. Please specify an empty folder.";
    public const string ProjectInitialized = "Project initialized successfully in '{ProjectPath}'.";
}