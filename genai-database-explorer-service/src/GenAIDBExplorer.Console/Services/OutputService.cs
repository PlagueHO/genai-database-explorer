namespace GenAIDBExplorer.Console.Services;

/// <summary>
/// This class is responsible for writing messages to the console.
/// </summary>
public class OutputService : IOutputService
{
    public void WriteLine(string message)
    {
        System.Console.WriteLine(message);
    }

    public void WriteWarning(string message)
    {
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine(message);
        System.Console.ResetColor();
    }

    public void WriteError(string message)
    {
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine(message);
        System.Console.ResetColor();
    }
}
