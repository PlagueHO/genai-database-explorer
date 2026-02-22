// Contract: IChatClientFactory
// Location: src/GenAIDBExplorer/GenAIDBExplorer.Core/ChatClients/IChatClientFactory.cs
// Purpose: Replaces ISemanticKernelFactory — creates AI clients from project configuration.

using Microsoft.Extensions.AI;

namespace GenAIDBExplorer.Core.ChatClients;

/// <summary>
/// Factory for creating AI chat clients and embedding generators from project configuration.
/// Replaces <c>ISemanticKernelFactory</c> as the central AI service creation point.
/// </summary>
public interface IChatClientFactory
{
    /// <summary>
    /// Creates a chat client for free-text generation (general descriptions).
    /// Uses the "ChatCompletion" deployment/model from project settings.
    /// </summary>
    /// <returns>An <see cref="IChatClient"/> configured for free-text generation.</returns>
    IChatClient CreateChatClient();

    /// <summary>
    /// Creates a chat client for structured output (JSON schema responses).
    /// Uses the "ChatCompletionStructured" deployment/model from project settings.
    /// </summary>
    /// <returns>An <see cref="IChatClient"/> configured for structured output.</returns>
    IChatClient CreateStructuredOutputChatClient();

    /// <summary>
    /// Creates an embedding generator for vector embedding operations.
    /// Uses the "Embedding" deployment/model from project settings.
    /// </summary>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> for generating embeddings.</returns>
    IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator();
}
