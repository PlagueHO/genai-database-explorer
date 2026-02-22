using Microsoft.Extensions.Configuration;

namespace GenAIDBExplorer.Core.Models.Project;

public interface IProject
{
    DirectoryInfo ProjectDirectory { get; }
    ProjectSettings Settings { get; }

    public void InitializeProjectDirectory(DirectoryInfo projectDirectory);

    public void LoadProjectConfiguration(DirectoryInfo projectDirectory);
}
