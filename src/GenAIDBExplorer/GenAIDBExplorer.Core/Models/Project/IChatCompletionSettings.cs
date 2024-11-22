namespace GenAIDBExplorer.Core.Models.Project;

internal interface IChatCompletionSettings
{
    string ServiceType { get; set; }
    string ModelId { get; set; }
    public string? OpenAIKey { get; set; }

    public string? AzureOpenAIKey { get; set; }

    public string? AzureOpenAIEndpoint { get; set; }

    public string? AzureOpenAIAppId { get; set; }

    public string? AzureOpenAIDeploymentId { get; set; }
}
