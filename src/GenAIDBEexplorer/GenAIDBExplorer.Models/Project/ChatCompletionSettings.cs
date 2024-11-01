using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.Project;

public class ChatCompletionSettings
{
    // The settings key that contains the ChatCompletion settings
    public const string PropertyName = "ChatCompletion";

    /// <summary>
    /// The service type to use for ChatCompletion
    /// </summary>
    public OpenAISeriviceType ServiceType { get; set; } = OpenAISeriviceType.OpenAI;

    public string? OpenAIKey { get; set; }

    public string? AzureOpenAIKey { get; set; }

    public string? AzureOpenAIEndpoint { get; set; }

    public string? AzureOpenAIAppId { get; set; }

    public string? AzureOpenAIDeploymentId { get; set; }
}
