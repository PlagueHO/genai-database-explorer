using Microsoft.Extensions.AI;
using Scriban;
using Scriban.Runtime;

namespace GenAIDBExplorer.Core.PromptTemplates;

/// <summary>
/// Renders Liquid template syntax using Scriban's ParseLiquid mode.
/// </summary>
public sealed class LiquidTemplateRenderer : ILiquidTemplateRenderer
{
    /// <inheritdoc />
    public IReadOnlyList<ChatMessage> RenderMessages(
        PromptTemplateDefinition definition,
        IDictionary<string, object?> variables)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(variables);

        var messages = new List<ChatMessage>();
        foreach (var templateMessage in definition.Messages)
        {
            var rendered = Render(templateMessage.ContentTemplate, variables);
            messages.Add(new ChatMessage(templateMessage.Role, rendered));
        }

        return messages;
    }

    /// <inheritdoc />
    public string Render(string liquidTemplate, IDictionary<string, object?> variables)
    {
        ArgumentNullException.ThrowIfNull(liquidTemplate);
        ArgumentNullException.ThrowIfNull(variables);

        var template = Template.ParseLiquid(liquidTemplate);

        if (template.HasErrors)
        {
            var errors = string.Join("; ", template.Messages.Select(m => m.ToString()));
            throw new InvalidOperationException($"Liquid template syntax error: {errors}");
        }

        var scriptObject = new ScriptObject();
        foreach (var kvp in variables)
        {
            scriptObject[kvp.Key] = kvp.Value;
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        // Enable member access on .NET objects using original member names
        context.MemberRenamer = member => member.Name;

        var result = template.Render(context);
        return result;
    }
}
