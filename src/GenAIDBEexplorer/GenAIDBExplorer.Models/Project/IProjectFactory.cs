using GenAIDBExplorer.Models.Project;
using Microsoft.Extensions.DependencyInjection;

namespace GenAIDBExplorer.Models.Project;

public interface IProjectFactory
{
    IProject Create(DirectoryInfo projectPath);
}
