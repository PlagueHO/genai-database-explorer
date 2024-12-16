using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Core.Models.SemanticModel.JsonConverters;

public class SemanticModelTableJsonConverter() : JsonConverter<SemanticModelTable>
{
    public override SemanticModelTable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Deserialization is not implemented.");
    }

    public override void Write(Utf8JsonWriter writer, SemanticModelTable value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Name", value.Name);
        writer.WriteString("Schema", value.Schema);
        writer.WriteString("Path", value.GetModelPath().Name);

        writer.WriteEndObject();
    }
}
