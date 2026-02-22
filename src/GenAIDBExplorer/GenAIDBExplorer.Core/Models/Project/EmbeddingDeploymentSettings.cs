namespace GenAIDBExplorer.Core.Models.Project;

public class EmbeddingDeploymentSettings : IEmbeddingDeploymentSettings
{
    // The settings key that contains the Embedding settings
    public const string PropertyName = "Embedding";

    public string? DeploymentName { get; set; }
}
