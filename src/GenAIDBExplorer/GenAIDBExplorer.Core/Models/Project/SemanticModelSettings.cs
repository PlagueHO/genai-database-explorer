using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class SemanticModelSettings
{
    // The settings key that contains the SemanticModel settings
    public const string PropertyName = "SemanticModel";

    /// <summary>
    /// Gets or sets the persistence strategy for semantic models.
    /// Valid values: LocalDisk, AzureBlob, Cosmos
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string PersistenceStrategy { get; set; } = "LocalDisk";

    /// <summary>
    /// The maximum number of parallel semantic model processes to run.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 1;
}
