using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.ProjectLogger;

public class ProjectLogger : ILogger
{
    private readonly IProject _project;
    private readonly string _categoryName;
    private readonly string _logName;
    private readonly string _logFilePath;

    public ProjectLogger(IProject project, string categoryName, string logName)
    {
        _project = project;
        _categoryName = categoryName;
        _logName = logName;

        // Build the log file path using the project directory
        var projectDir = _project.ProjectDirectory.FullName;
        var logDir = Path.Combine(projectDir, ".genaidbexplorer", "logs", DateTime.Now.ToString("yyyyMMdd"));
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, _logName);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true; // Adjust log level filtering if needed

    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception? exception, Func<TState, Exception?, string> formatter) where TState : notnull
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var logRecord = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {_categoryName}: {message}";

        try
        {
            File.AppendAllText(_logFilePath, logRecord + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Handle exceptions if necessary
        }
    }
}
