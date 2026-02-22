namespace GenAIDBExplorer.Core.Models.Project;

public class ChatCompletionStructuredDeploymentSettings : IChatCompletionDeploymentSettings
{
    // The settings key that contains the ChatCompletionStructured settings
    public const string PropertyName = "ChatCompletionStructured";

    public string? DeploymentName { get; set; }
}
