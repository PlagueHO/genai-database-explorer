using System.Security.Cryptography;
using System.Text;

namespace GenAIDBExplorer.Core.SemanticVectors.Keys;

public interface IEntityKeyBuilder
{
    string BuildKey(string modelName, string entityType, string schema, string name);
    string BuildContentHash(string content);
}
