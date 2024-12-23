using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class OpenAIServiceChatCompletionSettings : IOpenAIServiceChatCompletionSettings
{
    // The settings key that contains the ChatCompletion settings
    public const string PropertyName = "ChatCompletion";

    public string? ModelId { get; set; }

    public string? AzureOpenAIDeploymentId { get; set; }
}
