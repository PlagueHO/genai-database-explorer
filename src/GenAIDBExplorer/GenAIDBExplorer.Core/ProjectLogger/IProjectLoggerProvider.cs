using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.ProjectLogger;

public interface IProjectLoggerProvider
{
    ILogger CreateLogger(string categoryName, string logName);
}
