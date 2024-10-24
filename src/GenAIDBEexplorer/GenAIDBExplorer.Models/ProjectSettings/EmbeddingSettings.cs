using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.ProjectSettings;

internal class EmbeddingSettings
{
    // The settings key that contains the Embedding settings
    public const string PropertyName = "Embedding";

    /// <summary>
    /// The service type to use for Embedding
    /// </summary>
    public OpenAISeriviceType ServiceType { get; set; } = OpenAISeriviceType.OpenAI;

    public string? OpenAIKey { get; set; }

    public string? AzureOpenAIKey { get; set; }

    public string? AzureOpenAIEndpoint { get; set; }

    public string? AzureOpenAIAppId { get; set; }

    public string? AzureOpenAIDeploymentId { get; set; }

}
