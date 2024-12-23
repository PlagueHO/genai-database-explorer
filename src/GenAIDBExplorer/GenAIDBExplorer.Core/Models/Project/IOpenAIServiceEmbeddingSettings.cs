using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

internal interface IOpenAIServiceEmbeddingSettings
{
    string? ModelId { get; set; }

    string? AzureOpenAIDeploymentId { get; set; }
}
