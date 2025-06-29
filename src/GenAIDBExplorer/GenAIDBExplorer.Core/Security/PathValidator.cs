using System.Text;

namespace GenAIDBExplorer.Core.Security;

/// <summary>
/// Provides validation methods for file and directory paths to prevent security vulnerabilities.
/// </summary>
public static class PathValidator
{
    private static readonly string[] DangerousPathSegments = ["..", "~"];
    private static readonly string[] ReservedDeviceNames = [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];
    private const int MaxPathLength = 260; // Windows MAX_PATH limit
    private const int MaxPathLengthExtended = 32767; // Windows extended path limit

    /// <summary>
    /// Validates and sanitizes a directory path to prevent directory traversal attacks.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="allowExtendedPaths">Whether to allow extended paths longer than MAX_PATH.</param>
    /// <returns>A sanitized version of the path.</returns>
    /// <exception cref="ArgumentException">Thrown when the path contains dangerous elements.</exception>
    public static string ValidateAndSanitizePath(string path, bool allowExtendedPaths = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Normalize path separators
        var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);

        // Validate path length
        ValidatePathLength(normalizedPath, allowExtendedPaths);

        // Check for dangerous path segments (directory traversal)
        ValidateDangerousSegments(normalizedPath);

        // Validate characters
        ValidatePathCharacters(normalizedPath);

        // Check for reserved device names
        ValidateReservedNames(normalizedPath);

        // Unicode normalization for security
        normalizedPath = NormalizeUnicodePath(normalizedPath);

        // Ensure the path is rooted to prevent relative path attacks
        if (!Path.IsPathRooted(normalizedPath))
        {
            throw new ArgumentException($"Path must be an absolute path: {path}", nameof(path));
        }

        // Get the full path to resolve any remaining relative components
        var fullPath = Path.GetFullPath(normalizedPath);

        return fullPath;
    }

    /// <summary>
    /// Validates path length constraints.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="allowExtendedPaths">Whether to allow extended paths.</param>
    private static void ValidatePathLength(string path, bool allowExtendedPaths)
    {
        var maxLength = allowExtendedPaths ? MaxPathLengthExtended : MaxPathLength;
        
        if (path.Length > maxLength)
        {
            throw new ArgumentException($"Path exceeds maximum length of {maxLength} characters: {path}", nameof(path));
        }

        // Check individual path segments
        var segments = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment.Length > 255) // Most file systems have a 255 character limit per segment
            {
                throw new ArgumentException($"Path segment exceeds maximum length of 255 characters: {segment}", nameof(path));
            }
        }
    }

    /// <summary>
    /// Validates that the path doesn't contain dangerous segments.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    private static void ValidateDangerousSegments(string path)
    {
        foreach (var dangerousSegment in DangerousPathSegments)
        {
            if (path.Contains(dangerousSegment, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Path contains dangerous segment '{dangerousSegment}': {path}", nameof(path));
            }
        }

        // Additional checks for encoded dangerous characters
        var decodedPath = Uri.UnescapeDataString(path);
        if (!string.Equals(path, decodedPath, StringComparison.Ordinal))
        {
            // Re-check the decoded path for dangerous segments
            foreach (var dangerousSegment in DangerousPathSegments)
            {
                if (decodedPath.Contains(dangerousSegment, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Path contains encoded dangerous segment '{dangerousSegment}': {path}", nameof(path));
                }
            }
        }
    }

    /// <summary>
    /// Validates path characters for security.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    private static void ValidatePathCharacters(string path)
    {
        // For path validation, we need to be more careful than filename validation
        // Allow colons only in drive letter position (index 1) and path separators
        var invalidChars = new char[] { '<', '>', '"', '|', '?', '*' }
            .Concat(Enumerable.Range(0, 32).Select(i => (char)i)) // Control characters
            .ToArray();

        // Check each character, allowing colon only at index 1 (drive letter)
        for (int i = 0; i < path.Length; i++)
        {
            var c = path[i];
            if (invalidChars.Contains(c))
            {
                throw new ArgumentException($"Path contains invalid characters: {path}", nameof(path));
            }
            // Allow colon only as drive separator (at index 1)
            if (c == ':' && i != 1)
            {
                throw new ArgumentException($"Path contains invalid characters: {path}", nameof(path));
            }
        }

        // Check for null bytes or other dangerous Unicode characters
        if (path.Contains('\0'))
        {
            throw new ArgumentException($"Path contains null characters: {path}", nameof(path));
        }
    }

    /// <summary>
    /// Validates that path segments don't match reserved device names.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    private static void ValidateReservedNames(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var segment in segments)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(segment).ToUpperInvariant();
            if (ReservedDeviceNames.Contains(nameWithoutExtension))
            {
                throw new ArgumentException($"Path contains reserved device name '{nameWithoutExtension}': {path}", nameof(path));
            }
        }
    }

    /// <summary>
    /// Normalizes Unicode characters in the path for security.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    private static string NormalizeUnicodePath(string path)
    {
        try
        {
            // Normalize to prevent Unicode-based attacks
            var normalized = path.Normalize(NormalizationForm.FormC);
            
            // Check if normalization changed the path significantly
            if (!string.Equals(path, normalized, StringComparison.Ordinal))
            {
                // Log the normalization for security monitoring
                // In a real implementation, you might want to log this event
            }
            
            return normalized;
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Path contains invalid Unicode characters: {path}", nameof(path));
        }
    }

    /// <summary>
    /// Validates a path for concurrent operation safety.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is safe for concurrent operations; otherwise, false.</returns>
    public static bool IsPathSafeForConcurrentOperations(string path)
    {
        try
        {
            var validatedPath = ValidateAndSanitizePath(path);
            
            // Check if the path is on a network drive (which may have different locking behavior)
            var pathRoot = Path.GetPathRoot(validatedPath);
            if (string.IsNullOrEmpty(pathRoot))
                return false;

            var driveInfo = new DriveInfo(pathRoot);
            
            // Network drives may not support proper file locking
            if (driveInfo.DriveType == DriveType.Network)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that a child path is within the bounds of a parent directory.
    /// </summary>
    /// <param name="parentPath">The parent directory path.</param>
    /// <param name="childPath">The child path to validate.</param>
    /// <returns>True if the child path is within the parent directory; otherwise, false.</returns>
    public static bool IsPathWithinDirectory(string parentPath, string childPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(childPath);

        try
        {
            var normalizedParent = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar);
            var normalizedChild = Path.GetFullPath(childPath).TrimEnd(Path.DirectorySeparatorChar);

            return normalizedChild.StartsWith(normalizedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                   normalizedChild.Equals(normalizedParent, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a directory path and ensures it exists or can be created safely.
    /// </summary>
    /// <param name="directoryPath">The directory path to validate.</param>
    /// <returns>A DirectoryInfo object for the validated path.</returns>
    /// <exception cref="ArgumentException">Thrown when the path is invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the path is denied.</exception>
    public static DirectoryInfo ValidateDirectoryPath(string directoryPath)
    {
        var sanitizedPath = ValidateAndSanitizePath(directoryPath);

        try
        {
            var directoryInfo = new DirectoryInfo(sanitizedPath);

            // Test if we can access the parent directory
            if (directoryInfo.Parent != null && !directoryInfo.Parent.Exists)
            {
                throw new DirectoryNotFoundException($"Parent directory does not exist: {directoryInfo.Parent.FullName}");
            }

            return directoryInfo;
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
        {
            throw new ArgumentException($"Invalid directory path: {directoryPath}", nameof(directoryPath), ex);
        }
    }
}
