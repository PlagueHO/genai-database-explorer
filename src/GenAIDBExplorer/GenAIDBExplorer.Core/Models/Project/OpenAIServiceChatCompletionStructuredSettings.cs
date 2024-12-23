using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class OpenAIServiceChatCompletionStructuredSettings : IOpenAIServiceChatCompletionSettings
{
    // The settings key that contains the ChatCompletionStructured settings
    public const string PropertyName = "ChatCompletionStructured";

    public string? ModelId { get; set; }

    public string? AzureOpenAIDeploymentId { get; set; }
}
