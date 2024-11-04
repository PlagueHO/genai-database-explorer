using Microsoft.Extensions.Configuration;

namespace GenAIDBExplorer.Models.Project;

public interface IProject
{
    ProjectSettings Settings { get; }

    public void InitializeProjectDirectory(DirectoryInfo projectDirectory);

    public void LoadConfiguration(string projectPath);
}
