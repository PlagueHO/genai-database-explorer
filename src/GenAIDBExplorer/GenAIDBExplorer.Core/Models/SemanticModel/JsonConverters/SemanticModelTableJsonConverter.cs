using GenAIDBExplorer.Core.Models.SemanticModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenAIDBExplorer.Core.Models.SemanticModel.JsonConverters;

public class SemanticModelTableJsonConverter() : JsonConverter<SemanticModelTable>
{
    public override SemanticModelTable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        string? name = null;
        string? schema = null;
        string? path = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                reader.Read();

                switch (propertyName)
                {
                    case "Name":
                        name = reader.GetString();
                        break;
                    case "Schema":
                        schema = reader.GetString();
                        break;
                    case "Path":
                        path = reader.GetString();
                        break;
                    default:
                        reader.Skip(); // Skip unknown properties
                        break;
                }
            }
        }

        if (name == null || schema == null)
        {
            throw new JsonException("Required properties Name and Schema are missing");
        }

        // Create a minimal SemanticModelTable with just the basic metadata
        // The full details will be loaded lazily when needed
        var table = new SemanticModelTable(schema, name);

        return table;
    }

    public override void Write(Utf8JsonWriter writer, SemanticModelTable value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Name", value.Name);
        writer.WriteString("Schema", value.Schema);
        writer.WriteString("Path", Path.Combine(value.GetModelPath().Parent?.Name ?? "", value.GetModelPath().Name));

        writer.WriteEndObject();
    }
}
