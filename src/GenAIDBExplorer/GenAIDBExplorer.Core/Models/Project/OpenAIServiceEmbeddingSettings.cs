using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class OpenAIServiceEmbeddingSettings : IOpenAIServiceEmbeddingSettings
{
    // The settings key that contains the Embedding settings
    public const string PropertyName = "Embedding";

    public string? ModelId { get; set; }

    public string? AzureOpenAIDeploymentId { get; set; }
}
