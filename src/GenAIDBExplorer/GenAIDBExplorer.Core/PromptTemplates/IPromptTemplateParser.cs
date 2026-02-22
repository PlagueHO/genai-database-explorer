namespace GenAIDBExplorer.Core.PromptTemplates;

/// <summary>
/// Parses prompt template files containing YAML frontmatter metadata and
/// role-delimited message sections (system, user, assistant).
/// </summary>
public interface IPromptTemplateParser
{
    /// <summary>
    /// Parses a prompt template file from the specified path.
    /// </summary>
    /// <param name="filePath">Absolute path to the .prompt file.</param>
    /// <returns>A <see cref="PromptTemplateDefinition"/> containing metadata and messages.</returns>
    /// <exception cref="FileNotFoundException">The specified file does not exist.</exception>
    /// <exception cref="FormatException">The file has malformed YAML frontmatter or invalid structure.</exception>
    PromptTemplateDefinition ParseFromFile(string filePath);

    /// <summary>
    /// Parses a prompt template from raw text content.
    /// </summary>
    /// <param name="content">The raw template content including YAML frontmatter and messages.</param>
    /// <returns>A <see cref="PromptTemplateDefinition"/> containing metadata and messages.</returns>
    /// <exception cref="FormatException">The content has malformed YAML frontmatter or invalid structure.</exception>
    PromptTemplateDefinition Parse(string content);
}
