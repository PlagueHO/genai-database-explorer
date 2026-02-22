namespace GenAIDBExplorer.Console.Services;

/// <summary>
/// This interface is responsible for writing messages to the console.
/// </summary>
public interface IOutputService
{
    void WriteLine(string message);
    void WriteWarning(string message);
    void WriteError(string message);
}
