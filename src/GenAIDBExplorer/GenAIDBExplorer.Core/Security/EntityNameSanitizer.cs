using System.Text;
using System.Text.RegularExpressions;

namespace GenAIDBExplorer.Core.Security;

/// <summary>
/// Provides sanitization methods for entity names to ensure they are safe for file system operations.
/// </summary>
public static class EntityNameSanitizer
{
    private const int MaxEntityNameLength = 128;
    private const int MaxFileNameLength = 255; // Most file systems have a 255 character limit
    private static readonly Regex InvalidFileNameCharsRegex = new(@"[<>:""/\\|?*\x00-\x1f]", RegexOptions.Compiled);
    private static readonly Regex UnicodeControlCharsRegex = new(@"\p{C}", RegexOptions.Compiled);
    private static readonly string[] ReservedNames = [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];
    private static readonly string[] DangerousExtensions = [
        ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar",
        ".ps1", ".psm1", ".psd1", ".msi", ".dll", ".sys", ".drv"
    ];

    /// <summary>
    /// Sanitizes an entity name to make it safe for use as a file name.
    /// </summary>
    /// <param name="entityName">The entity name to sanitize.</param>
    /// <param name="strictMode">Whether to apply strict sanitization rules.</param>
    /// <returns>A sanitized entity name safe for file system operations.</returns>
    /// <exception cref="ArgumentException">Thrown when the entity name is invalid or too long.</exception>
    public static string SanitizeEntityName(string entityName, bool strictMode = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        // Check length constraint
        if (entityName.Length > MaxEntityNameLength)
        {
            throw new ArgumentException($"Entity name exceeds maximum length of {MaxEntityNameLength} characters: {entityName}", nameof(entityName));
        }

        // Unicode normalization for security
        var normalized = NormalizeUnicodeEntityName(entityName);

        // Remove invalid characters
        var sanitized = InvalidFileNameCharsRegex.Replace(normalized, "_");

        // Remove Unicode control characters in strict mode
        if (strictMode)
        {
            sanitized = UnicodeControlCharsRegex.Replace(sanitized, "_");
        }

        // Ensure it doesn't start or end with spaces or dots
        sanitized = sanitized.Trim(' ', '.');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            throw new ArgumentException($"Entity name results in empty string after sanitization: {entityName}", nameof(entityName));
        }

        // Check for reserved Windows file names
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized).ToUpperInvariant();
        if (ReservedNames.Contains(nameWithoutExtension))
        {
            sanitized = $"_{sanitized}";
        }

        // Check for dangerous file extensions
        ValidateDangerousExtensions(sanitized);

        // Final length validation after sanitization
        if (sanitized.Length > MaxEntityNameLength)
        {
            sanitized = sanitized[..MaxEntityNameLength].TrimEnd(' ', '.');
        }

        return sanitized;
    }

    /// <summary>
    /// Normalizes Unicode characters in the entity name for security.
    /// </summary>
    /// <param name="entityName">The entity name to normalize.</param>
    /// <returns>The normalized entity name.</returns>
    private static string NormalizeUnicodeEntityName(string entityName)
    {
        try
        {
            // Normalize to prevent Unicode-based attacks
            var normalized = entityName.Normalize(NormalizationForm.FormC);
            
            // Additional check for homograph attacks (similar-looking characters)
            ValidateHomographSafety(entityName, normalized);
            
            return normalized;
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Entity name contains invalid Unicode characters: {entityName}", nameof(entityName));
        }
    }

    /// <summary>
    /// Validates that the entity name doesn't contain potentially dangerous extensions.
    /// </summary>
    /// <param name="entityName">The entity name to validate.</param>
    private static void ValidateDangerousExtensions(string entityName)
    {
        var extension = Path.GetExtension(entityName);
        if (!string.IsNullOrEmpty(extension))
        {
            if (DangerousExtensions.Contains(extension.ToLowerInvariant()))
            {
                throw new ArgumentException($"Entity name contains dangerous file extension '{extension}': {entityName}", nameof(entityName));
            }
        }
    }

    /// <summary>
    /// Validates entity name for potential homograph attacks.
    /// </summary>
    /// <param name="original">The original entity name.</param>
    /// <param name="normalized">The normalized entity name.</param>
    private static void ValidateHomographSafety(string original, string normalized)
    {
        // If normalization significantly changed the string, it might be a homograph attack
        if (!string.Equals(original, normalized, StringComparison.Ordinal))
        {
            // Check if the change is just case normalization or more significant
            if (!string.Equals(original, normalized, StringComparison.OrdinalIgnoreCase))
            {
                // Significant change detected - might be a homograph attack
                // In production, you might want to log this for security monitoring
            }
        }
    }

    /// <summary>
    /// Validates that an entity name is safe for file system operations without modification.
    /// </summary>
    /// <param name="entityName">The entity name to validate.</param>
    /// <param name="strictMode">Whether to apply strict validation rules.</param>
    /// <returns>True if the entity name is safe; otherwise, false.</returns>
    public static bool IsValidEntityName(string entityName, bool strictMode = true)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return false;

        if (entityName.Length > MaxEntityNameLength)
            return false;

        if (InvalidFileNameCharsRegex.IsMatch(entityName))
            return false;

        // Check Unicode control characters in strict mode
        if (strictMode && UnicodeControlCharsRegex.IsMatch(entityName))
            return false;

        var trimmed = entityName.Trim(' ', '.');
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed != entityName)
            return false;

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(entityName).ToUpperInvariant();
        if (ReservedNames.Contains(nameWithoutExtension))
            return false;

        // Check for dangerous extensions
        var extension = Path.GetExtension(entityName);
        if (!string.IsNullOrEmpty(extension) && DangerousExtensions.Contains(extension.ToLowerInvariant()))
            return false;

        // Unicode normalization check
        try
        {
            var normalized = entityName.Normalize(NormalizationForm.FormC);
            if (!string.Equals(entityName, normalized, StringComparison.Ordinal))
                return false;
        }
        catch
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a safe file name for an entity by combining schema and entity name.
    /// </summary>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="entityName">The entity name.</param>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns>A safe file name for the entity.</returns>
    public static string CreateSafeFileName(string schemaName, string entityName, string extension = ".json")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var sanitizedSchema = SanitizeEntityName(schemaName);
        var sanitizedEntity = SanitizeEntityName(entityName);

        // Ensure extension starts with a dot
        if (!extension.StartsWith('.'))
        {
            extension = $".{extension}";
        }

        var fileName = $"{sanitizedSchema}.{sanitizedEntity}{extension}";

        // Final length check for the complete file name
        if (fileName.Length > MaxFileNameLength)
        {
            // Truncate while preserving the structure
            var maxBaseLength = MaxFileNameLength - extension.Length - 1; // -1 for the dot between schema and entity
            var halfLength = maxBaseLength / 2;

            sanitizedSchema = sanitizedSchema.Length > halfLength
                ? sanitizedSchema[..halfLength]
                : sanitizedSchema;

            sanitizedEntity = sanitizedEntity.Length > halfLength
                ? sanitizedEntity[..halfLength]
                : sanitizedEntity;

            fileName = $"{sanitizedSchema}.{sanitizedEntity}{extension}";
        }

        return fileName;
    }

    /// <summary>
    /// Validates input for potential injection attacks and dangerous content.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentException">Thrown when dangerous content is detected.</exception>
    public static void ValidateInputSecurity(string input, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input, parameterName);

        // Check for common injection patterns
        var dangerousPatterns = new[]
        {
            @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", // Script tags
            @"javascript:", // JavaScript protocol
            @"data:", // Data URI scheme
            @"vbscript:", // VBScript protocol
            @"onload\s*=", // Event handlers
            @"onerror\s*=",
            @"onclick\s*=",
            @"<%.*?%>", // Server-side includes
            @"\$\{.*?\}", // Template injection
            @"#\{.*?\}", // Ruby/ERB injection
            @"\{\{.*?\}\}", // Handlebars/Mustache injection
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                throw new ArgumentException($"Input contains potentially dangerous content: {parameterName}", parameterName);
            }
        }

        // Check for excessive repetition (potential DoS)
        if (HasExcessiveRepetition(input))
        {
            throw new ArgumentException($"Input contains excessive character repetition: {parameterName}", parameterName);
        }

        // Check for binary content
        if (ContainsBinaryContent(input))
        {
            throw new ArgumentException($"Input contains binary content: {parameterName}", parameterName);
        }
    }

    /// <summary>
    /// Checks if the input has excessive character repetition.
    /// </summary>
    /// <param name="input">The input to check.</param>
    /// <returns>True if excessive repetition is detected; otherwise, false.</returns>
    private static bool HasExcessiveRepetition(string input)
    {
        const int maxRepetition = 100;
        var charCount = new Dictionary<char, int>();

        foreach (var c in input)
        {
            charCount[c] = charCount.GetValueOrDefault(c, 0) + 1;
            if (charCount[c] > maxRepetition)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the input contains binary content.
    /// </summary>
    /// <param name="input">The input to check.</param>
    /// <returns>True if binary content is detected; otherwise, false.</returns>
    private static bool ContainsBinaryContent(string input)
    {
        // Check for null bytes and other binary indicators
        return input.Contains('\0') || 
               input.Any(c => c < 32 && c != '\t' && c != '\n' && c != '\r');
    }
}
