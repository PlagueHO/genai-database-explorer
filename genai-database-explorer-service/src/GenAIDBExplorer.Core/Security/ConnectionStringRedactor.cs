using System.Text.RegularExpressions;

namespace GenAIDBExplorer.Core.Security;

/// <summary>
/// Provides secure redaction of sensitive information in connection strings.
/// </summary>
public static class ConnectionStringRedactor
{
    private const string RedactedValue = "***REDACTED***";

    // Patterns to match various connection string password formats
    private static readonly Regex[] PasswordPatterns = [
        new Regex(@"password\s*=\s*[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"pwd\s*=\s*[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"pass\s*=\s*[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    // Patterns to match other sensitive connection string parameters
    private static readonly Regex[] SensitivePatterns = [
        new Regex(@"user\s+secret\s*=\s*[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"shared\s+access\s+key\s*=\s*[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"accountkey\s*=\s*[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"accesskey\s*=\s*[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"sharedaccesskey\s*=\s*[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    /// <summary>
    /// Redacts sensitive information (passwords, keys, secrets) from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to redact.</param>
    /// <returns>A connection string with sensitive information replaced with ***REDACTED***</returns>
    public static string RedactSensitiveInformation(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var result = connectionString;

        // Redact password patterns
        foreach (var pattern in PasswordPatterns)
        {
            result = pattern.Replace(result, match =>
            {
                var keyValuePair = match.Value;
                var equalIndex = keyValuePair.IndexOf('=');
                if (equalIndex > 0)
                {
                    var key = keyValuePair[..equalIndex].Trim();
                    return $"{key}={RedactedValue}";
                }
                return keyValuePair; // Fallback if parsing fails
            });
        }

        // Redact other sensitive patterns
        foreach (var pattern in SensitivePatterns)
        {
            result = pattern.Replace(result, match =>
            {
                var keyValuePair = match.Value;
                var equalIndex = keyValuePair.IndexOf('=');
                if (equalIndex > 0)
                {
                    var key = keyValuePair[..equalIndex].Trim();
                    return $"{key}={RedactedValue}";
                }
                return keyValuePair; // Fallback if parsing fails
            });
        }

        return result;
    }

    /// <summary>
    /// Checks if a connection string contains sensitive information that should be redacted.
    /// </summary>
    /// <param name="connectionString">The connection string to check.</param>
    /// <returns>True if the connection string contains sensitive information, false otherwise.</returns>
    public static bool ContainsSensitiveInformation(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        // Check for password patterns
        foreach (var pattern in PasswordPatterns)
        {
            if (pattern.IsMatch(connectionString))
            {
                return true;
            }
        }

        // Check for other sensitive patterns
        foreach (var pattern in SensitivePatterns)
        {
            if (pattern.IsMatch(connectionString))
            {
                return true;
            }
        }

        return false;
    }
}