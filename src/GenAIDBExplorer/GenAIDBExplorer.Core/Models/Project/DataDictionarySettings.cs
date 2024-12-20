using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class DataDictionarySettings
{
    // The settings key for DataDictionary settings
    public const string PropertyName = "DataDictionary";

    /// <summary>
    /// Column type mappings from data dictionary types to semantic model types.
    /// </summary>
    public List<ColumnTypeMapping> ColumnTypeMapping { get; set; } = new();
}

public class ColumnTypeMapping
{
    /// <summary>
    /// The original column type from the data dictionary.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// The column type to map to in the semantic model.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string To { get; set; } = string.Empty;
}
