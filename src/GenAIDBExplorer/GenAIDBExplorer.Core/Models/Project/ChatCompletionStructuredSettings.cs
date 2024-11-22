using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class ChatCompletionStructuredSettings : IChatCompletionSettings
{
    // The settings key that contains the ChatCompletion settings
    public const string PropertyName = "ChatCompletionStructured";

    /// <summary>
    /// The service type to use for Chat Completion (Structured)
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ServiceType { get; set; } = "AzureOpenAI";

    [RequiredOnPropertyValue(nameof(ServiceType), "OpenAI")]
    public string ModelId { get; set; } = "gpt-4o";

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
