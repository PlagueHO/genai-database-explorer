using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GenAIDBExplorer.Core.SemanticVectors.Keys;

public class EntityKeyBuilder : IEntityKeyBuilder
{
    public string BuildKey(string modelName, string entityType, string schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var parts = new[] { modelName, entityType, schema, name };
        var normalized = parts.Select(Normalize).ToArray();
        return string.Join(":", normalized);
    }

    public string BuildContentHash(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Normalize(string value)
    {
        value = value.Trim().ToLowerInvariant();
        // collapse whitespace
        value = Regex.Replace(value, @"\s+", " ");
        // remove disallowed chars, keep [a-z0-9-_:. ] then replace spaces with '-'
        value = Regex.Replace(value, @"[^a-z0-9_:\.\-]+", "");
        value = value.Replace(' ', '-');
        return value;
    }
}
