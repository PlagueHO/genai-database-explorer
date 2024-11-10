using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GenAIDBExplorer.Models.SemanticModel;

public static class SemanticModelEntityExtensions
{
    public static string ToYaml(this ISemanticModelEntity entity)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

        return serializer.Serialize(entity);
    }
}
