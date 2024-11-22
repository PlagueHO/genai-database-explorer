using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class SemanticModelSettings
{
    // The settings key that contains the Database settings
    public const string PropertyName = "SemanticModel";
    /// <summary>
    /// The maximum number of parallel semantic model processes to run.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 1;
}
