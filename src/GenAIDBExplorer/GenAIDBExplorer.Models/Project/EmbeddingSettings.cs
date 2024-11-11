using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Models.Project;

public class EmbeddingSettings
{
    // The settings key that contains the Embedding settings
    public const string PropertyName = "Embedding";

    /// <summary>
    /// The service type to use for Embedding
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ServiceType { get; set; } = "AzureOpenAI";

    [NotEmptyOrWhitespace]
    public string Model { get; set; } = "text-embedding-3-large";

    [RequiredOnPropertyValue(nameof(ServiceType), "OpenAI")]
    public string? OpenAIKey { get; set; }

    [RequiredOnPropertyValue(nameof(ServiceType), "AzureOpenAI")]
    public string? AzureOpenAIKey { get; set; }

    [RequiredOnPropertyValue(nameof(ServiceType), "AzureOpenAI")]
    public string? AzureOpenAIEndpoint { get; set; }

    public string? AzureOpenAIAppId { get; set; }

    [RequiredOnPropertyValue(nameof(ServiceType), "AzureOpenAI")]
    public string? AzureOpenAIDeploymentId { get; set; }
}
