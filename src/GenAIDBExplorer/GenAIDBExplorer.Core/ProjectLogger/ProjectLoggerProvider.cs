using Microsoft.Extensions.Logging;
using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Core.ProjectLogger;

public class ProjectLoggerProvider(
    IProject project
) : IProjectLoggerProvider
{
    private readonly IProject _project = project;

    public ILogger CreateLogger(string categoryName, string logName)
    {
        return new ProjectLogger(_project, categoryName, logName);
    }
}
