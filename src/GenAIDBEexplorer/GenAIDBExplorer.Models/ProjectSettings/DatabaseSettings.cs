using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Models.ProjectSettings;

public class DatabaseSettings
{
    // The settings key that contains the Database settings
    public const string PropertyName = "Database";

    /// <summary>
    /// Connection string to the database
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string? ConnectionString { get; set; }
}
