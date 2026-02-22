using Microsoft.Extensions.AI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GenAIDBExplorer.Core.PromptTemplates;

/// <summary>
/// Parses prompt template files with YAML frontmatter and role-delimited message sections.
/// </summary>
public sealed class PromptTemplateParser : IPromptTemplateParser
{
    private const string YamlDelimiter = "---";

    private static readonly string[] RoleMarkers = ["system:", "user:", "assistant:"];

    /// <inheritdoc />
    public PromptTemplateDefinition ParseFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Prompt template file not found: {filePath}", filePath);
        }

        var content = File.ReadAllText(filePath);
        return Parse(content);
    }

    /// <inheritdoc />
    public PromptTemplateDefinition Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var (yamlFrontmatter, body) = ExtractYamlFrontmatter(content);
        var (name, description, modelParameters) = ParseYamlMetadata(yamlFrontmatter);
        var messages = ParseMessages(body);

        return new PromptTemplateDefinition(name, description, modelParameters, messages);
    }

    private static (string yaml, string body) ExtractYamlFrontmatter(string content)
    {
        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith(YamlDelimiter))
        {
            throw new FormatException("Prompt template must start with YAML frontmatter delimited by '---'.");
        }

        var firstDelimiterEnd = trimmed.IndexOf('\n') + 1;
        var secondDelimiterStart = trimmed.IndexOf($"\n{YamlDelimiter}", firstDelimiterEnd, StringComparison.Ordinal);
        if (secondDelimiterStart < 0)
        {
            throw new FormatException("Malformed YAML frontmatter: closing '---' delimiter not found.");
        }

        var yaml = trimmed[firstDelimiterEnd..secondDelimiterStart].Trim();
        var bodyStart = trimmed.IndexOf('\n', secondDelimiterStart + 1);
        var body = bodyStart >= 0 ? trimmed[(bodyStart + 1)..] : string.Empty;

        return (yaml, body);
    }

    private static (string name, string? description, PromptTemplateModelParameters parameters) ParseYamlMetadata(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        Dictionary<string, object>? parsed;
        try
        {
            parsed = deserializer.Deserialize<Dictionary<string, object>>(yaml);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to parse YAML frontmatter: {ex.Message}", ex);
        }

        if (parsed is null)
        {
            throw new FormatException("YAML frontmatter is empty.");
        }

        var name = parsed.TryGetValue("name", out var nameValue) ? nameValue?.ToString() ?? string.Empty : string.Empty;
        var description = parsed.TryGetValue("description", out var descValue) ? descValue?.ToString() : null;

        double? temperature = null;
        double? topP = null;
        int? maxTokens = null;

        if (parsed.TryGetValue("model", out var modelObj) && modelObj is Dictionary<object, object> modelDict)
        {
            if (modelDict.TryGetValue("parameters", out var paramsObj) && paramsObj is Dictionary<object, object> paramsDict)
            {
                if (paramsDict.TryGetValue("temperature", out var tempVal))
                {
                    temperature = Convert.ToDouble(tempVal);
                }
                if (paramsDict.TryGetValue("top_p", out var topPVal))
                {
                    topP = Convert.ToDouble(topPVal);
                }
                if (paramsDict.TryGetValue("max_tokens", out var maxVal))
                {
                    maxTokens = Convert.ToInt32(maxVal);
                }
            }
        }

        return (name, description, new PromptTemplateModelParameters(temperature, topP, maxTokens));
    }

    private static List<PromptTemplateMessage> ParseMessages(string body)
    {
        var messages = new List<PromptTemplateMessage>();
        ChatRole? currentRole = null;
        var currentContent = new List<string>();

        foreach (var line in body.Split('\n'))
        {
            var trimmedLine = line.TrimEnd('\r');
            var matchedRole = TryMatchRoleMarker(trimmedLine);

            if (matchedRole.HasValue)
            {
                // Save previous message
                if (currentRole.HasValue)
                {
                    messages.Add(new PromptTemplateMessage(currentRole.Value, JoinContent(currentContent)));
                }

                currentRole = matchedRole.Value;
                currentContent.Clear();
            }
            else
            {
                currentContent.Add(trimmedLine);
            }
        }

        // Save last message
        if (currentRole.HasValue)
        {
            messages.Add(new PromptTemplateMessage(currentRole.Value, JoinContent(currentContent)));
        }

        return messages;
    }

    private static ChatRole? TryMatchRoleMarker(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Equals("system:", StringComparison.OrdinalIgnoreCase))
            return ChatRole.System;
        if (trimmed.Equals("user:", StringComparison.OrdinalIgnoreCase))
            return ChatRole.User;
        if (trimmed.Equals("assistant:", StringComparison.OrdinalIgnoreCase))
            return ChatRole.Assistant;
        return null;
    }

    private static string JoinContent(List<string> lines)
    {
        // Trim trailing empty lines, preserve leading whitespace structure
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        // Remove leading empty lines
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
        {
            lines.RemoveAt(0);
        }

        return string.Join("\n", lines);
    }
}
