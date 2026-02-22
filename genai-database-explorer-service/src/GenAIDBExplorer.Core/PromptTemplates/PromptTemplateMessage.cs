using Microsoft.Extensions.AI;

namespace GenAIDBExplorer.Core.PromptTemplates;

/// <summary>
/// A single message within a prompt template, consisting of a role and
/// a raw Liquid template string for the content.
/// </summary>
public sealed record PromptTemplateMessage(
    ChatRole Role,
    string ContentTemplate
);
