using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Repository.Caching;
using GenAIDBExplorer.Core.Repository.Performance;
using GenAIDBExplorer.Core.Repository.Security;
using GenAIDBExplorer.Core.SemanticModelQuery;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Policy;
using GenAIDBExplorer.Core.SemanticVectors.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Extensions;

/// <summary>
/// Shared service registrations used by both the Console and API projects.
/// Registers repository, persistence, caching, security, and performance monitoring services.
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the core shared services required by any host (Console or API).
    /// </summary>
    public static IServiceCollection AddGenAIDBExplorerCoreServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Azure persistence strategy options
        services.Configure<AzureBlobConfiguration>(
            configuration.GetSection(AzureBlobConfiguration.SectionName));
        services.Configure<CosmosDbConfiguration>(
            configuration.GetSection(CosmosDbConfiguration.SectionName));
        services.Configure<LocalDiskConfiguration>(
            configuration.GetSection(LocalDiskConfiguration.SectionName));

        // Configure caching options
        services.Configure<CacheOptions>(
            configuration.GetSection(CacheOptions.SectionName));

        // Configure security options
        services.Configure<SecureJsonSerializerOptions>(
            configuration.GetSection("SecureJsonSerializer"));
        services.Configure<KeyVaultOptions>(
            configuration.GetSection("KeyVault"));

        // Register the Project service
        services.AddSingleton<IProject, Project>();

        services.AddMemoryCache();
        services.AddSingleton<ISemanticModelCache, MemorySemanticModelCache>();

        services.AddSingleton<ISecureJsonSerializer, SecureJsonSerializer>();

        // Register Key Vault provider if enabled
        services.AddSingleton<KeyVaultConfigurationProvider>(provider =>
        {
            var options = provider.GetRequiredService<IConfiguration>()
                .GetSection("KeyVault").Get<KeyVaultOptions>();

            if (options?.EnableKeyVault == true && !string.IsNullOrWhiteSpace(options.KeyVaultUri))
            {
                var logger = provider.GetRequiredService<ILogger<KeyVaultConfigurationProvider>>();
                return new KeyVaultConfigurationProvider(options.KeyVaultUri, logger);
            }

            return null!;
        });

        // Register persistence strategies
        services.AddSingleton<ILocalDiskPersistenceStrategy, LocalDiskPersistenceStrategy>();

        services.AddSingleton<IAzureBlobPersistenceStrategy>(serviceProvider =>
        {
            var project = serviceProvider.GetRequiredService<IProject>();
            var logger = serviceProvider.GetRequiredService<ILogger<AzureBlobPersistenceStrategy>>();
            var secureJsonSerializer = serviceProvider.GetRequiredService<ISecureJsonSerializer>();
            var keyVaultProvider = serviceProvider.GetService<KeyVaultConfigurationProvider>();

            var azureBlobConfig = project.Settings.SemanticModelRepository?.AzureBlob
                ?? throw new InvalidOperationException(
                    "AzureBlob configuration is required when using AzureBlobPersistenceStrategy. " +
                    "Ensure SemanticModelRepository.AzureBlob is configured in project settings.json.");

            return new AzureBlobPersistenceStrategy(azureBlobConfig, logger, secureJsonSerializer, keyVaultProvider);
        });

        services.AddSingleton<ICosmosDbPersistenceStrategy>(serviceProvider =>
        {
            var project = serviceProvider.GetRequiredService<IProject>();
            var logger = serviceProvider.GetRequiredService<ILogger<CosmosDbPersistenceStrategy>>();
            var secureJsonSerializer = serviceProvider.GetRequiredService<ISecureJsonSerializer>();
            var keyVaultProvider = serviceProvider.GetService<KeyVaultConfigurationProvider>();

            var cosmosDbConfig = project.Settings.SemanticModelRepository?.CosmosDb
                ?? throw new InvalidOperationException(
                    "CosmosDb configuration is required when using CosmosDbPersistenceStrategy. " +
                    "Ensure SemanticModelRepository.CosmosDb is configured in project settings.json.");

            return new CosmosDbPersistenceStrategy(cosmosDbConfig, logger, secureJsonSerializer, keyVaultProvider);
        });

        // Register performance monitoring services
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();

        // Repository, Options Builders and persistence strategies
        services.AddSingleton<IPersistenceStrategyFactory, PersistenceStrategyFactory>();
        services.AddSingleton<ISemanticModelRepository, SemanticModelRepository>();

        services.AddTransient<ISemanticModelRepositoryOptionsBuilder>(
            _ => SemanticModelRepositoryOptionsBuilder.Create());

        services.AddTransient<IPerformanceMonitoringOptionsBuilder>(
            _ => PerformanceMonitoringOptionsBuilder.Create());

        return services;
    }

    /// <summary>
    /// Registers the vector search services required for the search endpoint.
    /// Includes embedding generation and vector similarity search, but excludes
    /// generation-only services (such as <c>IVectorGenerationService</c>).
    /// </summary>
    public static IServiceCollection AddGenAIDBExplorerVectorSearchServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IChatClientFactory, ChatClientFactory>();
        services.AddSingleton<IVectorIndexPolicy, VectorIndexPolicy>();
        services.AddSingleton<IVectorInfrastructureFactory, VectorInfrastructureFactory>();
        services.AddSingleton<IEmbeddingGenerator, ChatClientEmbeddingGenerator>();
        services.AddSingleton<Microsoft.SemanticKernel.Connectors.InMemory.InMemoryVectorStore>();
        services.AddSingleton<IVectorSearchService, SkInMemoryVectorSearchService>();
        services.AddSingleton<ISemanticModelSearchService, SemanticModelSearchService>();

        return services;
    }
}
