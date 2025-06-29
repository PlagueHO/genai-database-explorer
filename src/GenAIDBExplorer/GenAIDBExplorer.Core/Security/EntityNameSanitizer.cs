using System.Text.RegularExpressions;

namespace GenAIDBExplorer.Core.Security;

/// <summary>
/// Provides sanitization methods for entity names to ensure they are safe for file system operations.
/// </summary>
public static class EntityNameSanitizer
{
    private const int MaxEntityNameLength = 128;
    private static readonly Regex InvalidFileNameCharsRegex = new(@"[<>:""/\\|?*\x00-\x1f]", RegexOptions.Compiled);
    private static readonly string[] ReservedNames = [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];

    /// <summary>
    /// Sanitizes an entity name to make it safe for use as a file name.
    /// </summary>
    /// <param name="entityName">The entity name to sanitize.</param>
    /// <returns>A sanitized entity name safe for file system operations.</returns>
    /// <exception cref="ArgumentException">Thrown when the entity name is invalid or too long.</exception>
    public static string SanitizeEntityName(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        // Check length constraint
        if (entityName.Length > MaxEntityNameLength)
        {
            throw new ArgumentException($"Entity name exceeds maximum length of {MaxEntityNameLength} characters: {entityName}", nameof(entityName));
        }

        // Remove invalid characters
        var sanitized = InvalidFileNameCharsRegex.Replace(entityName, "_");

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

        return sanitized;
    }

    /// <summary>
    /// Validates that an entity name is safe for file system operations without modification.
    /// </summary>
    /// <param name="entityName">The entity name to validate.</param>
    /// <returns>True if the entity name is safe; otherwise, false.</returns>
    public static bool IsValidEntityName(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return false;

        if (entityName.Length > MaxEntityNameLength)
            return false;

        if (InvalidFileNameCharsRegex.IsMatch(entityName))
            return false;

        var trimmed = entityName.Trim(' ', '.');
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed != entityName)
            return false;

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(entityName).ToUpperInvariant();
        if (ReservedNames.Contains(nameWithoutExtension))
            return false;

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
        if (fileName.Length > 255) // Most file systems have a 255 character limit
        {
            // Truncate while preserving the structure
            var maxBaseLength = 255 - extension.Length - 1; // -1 for the dot between schema and entity
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
}
