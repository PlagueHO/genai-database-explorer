using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace GenAIDBExplorer.Console.Logger;

/// <summary>
/// A logging provider that redacts sensitive information such as passwords from log messages.
/// </summary>
public class RedactingLoggerProvider : ILoggerProvider
{
    /// <summary>
    /// Creates a new instance of the <see cref="RedactingLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>A new instance of the <see cref="RedactingLogger"/> class.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return new RedactingLogger();
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="RedactingLoggerProvider"/> class.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// A logger that redacts sensitive information such as passwords from log messages.
    /// </summary>
    private class RedactingLogger : ILogger
    {
        private static readonly Regex PasswordRegex = new Regex(@"(Password\s*=\s*)[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state) => null;

        /// <summary>
        /// Checks if the given log level is enabled.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns><c>true</c> if the log level is enabled; otherwise, <c>false</c>.</returns>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">The log level.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="state">The state object.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="formatter">The function to create a log message.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter != null)
            {
                var message = formatter(state, exception);
                var sanitizedMessage = PasswordRegex.Replace(message, "$1********");
                System.Console.WriteLine(sanitizedMessage);
            }
        }
    }
}