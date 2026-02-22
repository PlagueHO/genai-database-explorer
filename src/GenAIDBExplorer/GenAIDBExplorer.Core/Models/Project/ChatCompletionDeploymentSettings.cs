namespace GenAIDBExplorer.Core.Models.Project;

public class ChatCompletionDeploymentSettings : IChatCompletionDeploymentSettings
{
    // The settings key that contains the ChatCompletion settings
    public const string PropertyName = "ChatCompletion";

    public string? DeploymentName { get; set; }
}
