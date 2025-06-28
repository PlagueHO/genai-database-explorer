namespace GenAIDBExplorer.Core.Security;

/// <summary>
/// Provides validation methods for file and directory paths to prevent security vulnerabilities.
/// </summary>
public static class PathValidator
{
    private static readonly string[] DangerousPathSegments = ["..", "~"];
    
    /// <summary>
    /// Validates and sanitizes a directory path to prevent directory traversal attacks.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>A sanitized version of the path.</returns>
    /// <exception cref="ArgumentException">Thrown when the path contains dangerous elements.</exception>
    public static string ValidateAndSanitizePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        
        // Normalize path separators
        var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);
        
        // Check for dangerous path segments (directory traversal)
        foreach (var dangerousSegment in DangerousPathSegments)
        {
            if (normalizedPath.Contains(dangerousSegment, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Path contains dangerous segment '{dangerousSegment}': {path}", nameof(path));
            }
        }
        
        // For path validation, we need to be more careful than filename validation
        // Allow colons only in drive letter position (index 1) and path separators
        var invalidChars = new char[] { '<', '>', '"', '|', '?', '*' }
            .Concat(Enumerable.Range(0, 32).Select(i => (char)i)) // Control characters
            .ToArray();
            
        // Check each character, allowing colon only at index 1 (drive letter)
        for (int i = 0; i < normalizedPath.Length; i++)
        {
            var c = normalizedPath[i];
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
