using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GenAIDBExplorer.Models.SemanticModel;

/// <summary>
/// Provides extension methods for <see cref="ISemanticModelEntity"/> to convert to YAML format.
/// </summary>
public static class SemanticModelEntityExtensions
{
    /// <summary>
    /// Converts the <see cref="ISemanticModelEntity"/> to a YAML string.
    /// </summary>
    /// <param name="entity">The semantic model entity to convert.</param>
    /// <returns>A YAML string representation of the entity.</returns>
    public static string ToYaml(this ISemanticModelEntity entity)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

        return serializer.Serialize(entity);
    }
}
