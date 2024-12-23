using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class OpenAIServiceDefaultSettings
{
    // The settings key that contains the Default OpenAI settings
    public const string PropertyName = "Default";

    [Required, NotEmptyOrWhitespace]
    public string ServiceType { get; set; } = "AzureOpenAI";

    [RequiredOnPropertyValue(nameof(ServiceType), "OpenAI")]
    public string? OpenAIKey { get; set; }

    [RequiredOnPropertyValue(nameof(ServiceType), "AzureOpenAI")]
    public string? AzureOpenAIKey { get; set; }

    [RequiredOnPropertyValue(nameof(ServiceType), "AzureOpenAI")]
    public string? AzureOpenAIEndpoint { get; set; }

    public string? AzureOpenAIAppId { get; set; }
}
