using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Models.Project;

public class ChatCompletionSettings
{
    // The settings key that contains the ChatCompletion settings
    public const string PropertyName = "ChatCompletion";

    /// <summary>
    /// The service type to use for ChatCompletion
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ServiceType { get; set; } = "AzureOpenAI";

    [NotEmptyOrWhitespace]
    public string Model { get; set; } = "gpt-4o";

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
