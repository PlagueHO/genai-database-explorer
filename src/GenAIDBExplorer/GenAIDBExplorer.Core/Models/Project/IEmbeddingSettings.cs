using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

internal interface IEmbeddingSettings
{
    public string ServiceType { get; set; }

    public string Model { get; set; }

    public string? OpenAIKey { get; set; }

    public string? AzureOpenAIKey { get; set; }

    public string? AzureOpenAIEndpoint { get; set; }

    public string? AzureOpenAIAppId { get; set; }

    public string? AzureOpenAIDeploymentId { get; set; }
}
