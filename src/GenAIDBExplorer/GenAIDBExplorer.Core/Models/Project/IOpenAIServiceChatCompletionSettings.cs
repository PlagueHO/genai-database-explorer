namespace GenAIDBExplorer.Core.Models.Project;

internal interface IOpenAIServiceChatCompletionSettings
{
    string? ModelId { get; set; }

    string? AzureOpenAIDeploymentId { get; set; }
}
