using Azure.AI.Projects;
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
    /// Uses the same "ChatCompletion" deployment as <see cref="CreateChatClient"/>,
    /// since all current models support structured output.
    /// </summary>
    /// <returns>An <see cref="IChatClient"/> configured for structured output.</returns>
    IChatClient CreateStructuredOutputChatClient();

    /// <summary>
    /// Creates an embedding generator for vector embedding operations.
    /// Uses the "Embedding" deployment/model from project settings.
    /// </summary>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> for generating embeddings.</returns>
    IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator();

    /// <summary>
    /// Gets the <see cref="AIProjectClient"/> for the configured Microsoft Foundry project endpoint.
    /// Used for agent service access and other Foundry project operations.
    /// Only available with Entra ID authentication.
    /// </summary>
    /// <returns>An <see cref="AIProjectClient"/> connected to the configured Foundry project.</returns>
    /// <exception cref="InvalidOperationException">Thrown when using API key authentication.</exception>
    AIProjectClient GetProjectClient();
}
