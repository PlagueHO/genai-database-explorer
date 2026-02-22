// Contract: ILiquidTemplateRenderer
// Location: src/GenAIDBExplorer/GenAIDBExplorer.Core/PromptTemplates/ILiquidTemplateRenderer.cs
// Purpose: Renders Liquid templates within prompt messages using Scriban.

using Microsoft.Extensions.AI;

namespace GenAIDBExplorer.Core.PromptTemplates;

/// <summary>
/// Renders Liquid template syntax in prompt template messages, producing
/// fully resolved <see cref="ChatMessage"/> instances ready for AI submission.
/// </summary>
public interface ILiquidTemplateRenderer
{
    /// <summary>
    /// Renders all messages in a prompt template definition, substituting Liquid variables
    /// and expanding loops, producing an ordered list of <see cref="ChatMessage"/>.
    /// </summary>
    /// <param name="definition">The parsed prompt template definition.</param>
    /// <param name="variables">A dictionary of variable names to values for Liquid substitution.</param>
    /// <returns>An ordered list of <see cref="ChatMessage"/> with rendered content.</returns>
    /// <exception cref="InvalidOperationException">A Liquid template contains syntax errors.</exception>
    IReadOnlyList<ChatMessage> RenderMessages(PromptTemplateDefinition definition, IDictionary<string, object?> variables);

    /// <summary>
    /// Renders a single Liquid template string with the given variables.
    /// </summary>
    /// <param name="liquidTemplate">The Liquid template text.</param>
    /// <param name="variables">A dictionary of variable names to values for Liquid substitution.</param>
    /// <returns>The rendered string.</returns>
    string Render(string liquidTemplate, IDictionary<string, object?> variables);
}
