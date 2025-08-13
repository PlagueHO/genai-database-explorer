using System;

namespace GenAIDBExplorer.Core.Repository
{
    /// <summary>
    /// Interface for Cosmos DB persistence strategy that extends the base semantic model persistence capabilities
    /// with Azure Cosmos DB-specific features and disposable resource management.
    /// </summary>
    /// <remarks>
    /// This interface extends ISemanticModelPersistenceStrategy to provide a specialized contract
    /// for Cosmos DB implementations. It includes IDisposable for proper resource cleanup
    /// of Cosmos DB connections and client resources.
    /// 
    /// Implementations should:
    /// - Use DefaultAzureCredential for authentication (supports managed identities)
    /// - Implement efficient partition key design for optimal performance
    /// - Include proper error handling with retry policies
    /// - Support concurrent operations with proper throttling
    /// - Dispose of CosmosClient and related resources properly
    /// 
    /// This interface allows for dependency injection scenarios where specific Cosmos DB
    /// features or resource management capabilities are required beyond the base persistence contract.
    /// </remarks>
    public interface ICosmosPersistenceStrategy : ISemanticModelPersistenceStrategy, IDisposable
    {
        // This interface extends the base persistence strategy with disposable resource management
        // All core persistence methods are inherited from ISemanticModelPersistenceStrategy
        // Additional Cosmos DB-specific methods can be added here in the future if needed
    }
}
