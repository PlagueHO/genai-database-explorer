using System.IO;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository.Helpers;

/// <summary>
/// Manages temporary directory lifecycle for semantic model operations.
/// </summary>
internal sealed class TempDirectoryManager : IDisposable
{
    private readonly DirectoryInfo _tempDirectory;
    private readonly ILogger _logger;
    private bool _disposed = false;

    public TempDirectoryManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{LocalDiskPersistenceConstants.FilePatterns.TempDirectoryPrefix}{Guid.NewGuid():N}");
        _tempDirectory = new DirectoryInfo(tempPath);

        _logger.LogDebug("Created temp directory manager for path: {TempPath}", _tempDirectory.FullName);
    }

    /// <summary>
    /// Gets the temporary directory path.
    /// </summary>
    public DirectoryInfo Path => _tempDirectory;

    /// <summary>
    /// Creates the temporary directory if it doesn't exist.
    /// </summary>
    public void EnsureExists()
    {
        if (!_tempDirectory.Exists)
        {
            _tempDirectory.Create();
            _logger.LogDebug("Created temporary directory: {TempPath}", _tempDirectory.FullName);
        }
    }

    /// <summary>
    /// Moves all contents from the temporary directory to the target directory atomically.
    /// </summary>
    public async Task MoveContentsToAsync(DirectoryInfo destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        await Task.Run(() =>
        {
            foreach (var file in _tempDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var relativePath = System.IO.Path.GetRelativePath(_tempDirectory.FullName, file.FullName);
                var destPath = System.IO.Path.Combine(destination.FullName, relativePath);

                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destPath)!);
                File.Move(file.FullName, destPath, true);
            }
        });

        _logger.LogDebug("Moved temp directory contents from {Source} to {Destination}",
            _tempDirectory.FullName, destination.FullName);
    }

    public void Dispose()
    {
        if (!_disposed && _tempDirectory.Exists)
        {
            try
            {
                _tempDirectory.Delete(recursive: true);
                _logger.LogDebug("Cleaned up temporary directory: {TempPath}", _tempDirectory.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary directory: {TempPath}", _tempDirectory.FullName);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}