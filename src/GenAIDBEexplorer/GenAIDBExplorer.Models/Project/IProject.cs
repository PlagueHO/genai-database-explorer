using Microsoft.Extensions.Configuration;

namespace GenAIDBExplorer.Models.Project
{
    public interface IProject
    {
        IConfiguration Configuration { get; }
        ChatCompletionSettings ChatCompletionSettings { get; }
        DatabaseSettings DatabaseSettings { get; }
        EmbeddingSettings EmbeddingSettings { get; }
    }
}
