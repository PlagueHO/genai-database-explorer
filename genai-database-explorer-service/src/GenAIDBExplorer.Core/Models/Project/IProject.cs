using Microsoft.Extensions.Configuration;

namespace GenAIDBExplorer.Core.Models.Project;

public interface IProject
{
    DirectoryInfo ProjectDirectory { get; }
    ProjectSettings Settings { get; }

    public void InitializeProjectDirectory(DirectoryInfo projectDirectory);

    public void LoadProjectConfiguration(DirectoryInfo projectDirectory);

    /// <summary>
    /// Gets the semantic model path based on the configured persistence strategy.
    /// For LocalDisk, returns the configured directory combined with the project directory.
    /// For other strategies, returns the project directory.
    /// </summary>
    public DirectoryInfo GetSemanticModelPath();
}
