using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Console.CommandHandlers;

/// <summary>
/// Command handler for initializing a project.
/// </summary>
/// <remarks>
/// This class implements the <see cref="ICommandHandler"/> interface and provides functionality to handle initialization commands.
/// </remarks>
public class InitCommandHandler : CommandHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
    public InitCommandHandler(ILogger<ICommandHandler> logger) : base(logger) { }

    /// <summary>
    /// Handles the initialization command with the specified project path.
    /// </summary>
    /// <param name="projectDirectory">The directory path of the project to initialize.</param>
    public override void Handle(DirectoryInfo projectDirectory)
    {
        _logger.LogInformation($"Initializing project at '{projectDirectory.FullName}'.");

        ValidateProjectPath(projectDirectory);

        if (IsDirectoryNotEmpty(projectDirectory))
        {
            System.Console.WriteLine("The project folder is not empty. Please specify an empty folder.");
            return;
        }

        InitializeProjectDirectory(projectDirectory);

        System.Console.WriteLine($"Project initialized successfully in '{projectDirectory.FullName}'.");
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

