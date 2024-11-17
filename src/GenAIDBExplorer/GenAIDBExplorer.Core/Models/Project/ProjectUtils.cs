namespace GenAIDBExplorer.Core.Models.Project;

internal static class ProjectUtils
{
    /// <summary>
    /// Checks if the specified directory is not empty.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <returns>True if the directory is not empty; otherwise, false.</returns>
    internal static bool IsDirectoryNotEmpty(DirectoryInfo directory)
    {
        return directory.EnumerateFileSystemInfos().Any();
    }

    /// <summary>
    /// Copies the contents of one directory to another directory.
    /// </summary>
    /// <param name="sourceDirectory">The source directory to copy from.</param>
    /// <param name="destinationDirectory">The destination directory to copy to.</param>
    /// <remarks>
    /// This method copies all files and subdirectories from the source directory to the destination directory.
    /// </remarks>
    internal static void CopyDirectory(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
    {
        if (!destinationDirectory.Exists)
        {
            destinationDirectory.Create();
        }

        CopyFiles(sourceDirectory, destinationDirectory);
        CopySubDirectories(sourceDirectory, destinationDirectory);
    }

    /// <summary>
    /// Copies files from the source directory to the destination directory.
    /// </summary>
    /// <param name="sourceDirectory">The source directory to copy from.</param>
    /// <param name="destinationDirectory">The destination directory to copy to.</param>
    internal static void CopyFiles(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
    {
        foreach (var file in sourceDirectory.GetFiles())
        {
            var destinationFilePath = Path.Combine(destinationDirectory.FullName, file.Name);
            file.CopyTo(destinationFilePath, overwrite: true);
        }
    }

    /// <summary>
    /// Copies subdirectories from the source directory to the destination directory.
    /// </summary>
    /// <param name="sourceDirectory">The source directory to copy from.</param>
    /// <param name="destinationDirectory">The destination directory to copy to.</param>
    internal static void CopySubDirectories(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
    {
        foreach (var subDirectory in sourceDirectory.GetDirectories())
        {
            var destinationSubDirectory = destinationDirectory.CreateSubdirectory(subDirectory.Name);
            CopyDirectory(subDirectory, destinationSubDirectory);
        }
    }
}
