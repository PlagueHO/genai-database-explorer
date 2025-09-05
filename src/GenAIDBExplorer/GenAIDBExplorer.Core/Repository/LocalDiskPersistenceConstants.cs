namespace GenAIDBExplorer.Core.Repository;

/// <summary>
/// Constants for LocalDisk persistence operations.
/// </summary>
internal static class LocalDiskPersistenceConstants
{
    /// <summary>
    /// The main semantic model file name.
    /// </summary>
    public const string SemanticModelFileName = "semanticmodel.json";

    /// <summary>
    /// The lock file name used during delete operations.
    /// </summary>
    public const string DeleteLockFileName = ".delete_lock";

    /// <summary>
    /// Folder names for different entity types.
    /// </summary>
    public static class Folders
    {
        public const string Tables = "tables";
        public const string Views = "views";
        public const string StoredProcedures = "storedprocedures";
    }

    /// <summary>
    /// File naming patterns.
    /// </summary>
    public static class FilePatterns
    {
        public const string EntityFile = "{0}.{1}.json"; // Schema.Name.json
        public const string TempDirectoryPrefix = "semanticmodel_temp_";
    }
}