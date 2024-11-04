using Microsoft.Extensions.Configuration;

namespace GenAIDBExplorer.Models.Project;

public interface IProject
{
    ProjectSettings Settings { get; }

    public void LoadConfiguration(string projectPath);
}
