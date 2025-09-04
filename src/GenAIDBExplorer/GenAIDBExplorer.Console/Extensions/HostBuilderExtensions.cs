using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Core.Data.ConnectionManager;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.DataDictionary;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticKernel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.SemanticProviders;
using GenAIDBExplorer.Core.SemanticVectors.Infrastructure;
using GenAIDBExplorer.Core.SemanticVectors.Policy;
using GenAIDBExplorer.Core.SemanticVectors.Mapping;
using GenAIDBExplorer.Core.SemanticVectors.Embeddings;
using GenAIDBExplorer.Core.SemanticVectors.Indexing;
using GenAIDBExplorer.Core.SemanticVectors.Search;
using GenAIDBExplorer.Core.SemanticVectors.Keys;
using GenAIDBExplorer.Core.SemanticVectors.Orchestration;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Repository.Caching;
using GenAIDBExplorer.Core.Repository.Performance;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Console.Extensions;

/// <summary>
/// Extension methods for configuring the host builder.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the host application builder with the necessary services and configurations.
    /// </summary>
    /// <param name="builder">The <see cref="HostApplicationBuilder"/> instance.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The configured <see cref="HostApplicationBuilder"/> instance.</returns>
    public static HostApplicationBuilder ConfigureHost(this HostApplicationBuilder builder, string[] args)
    {
        // Determine the correct path for appsettings.json relative to the console project
        var candidateRoots = new[]
        {
            // When running from repo root
            Path.Combine("src", "GenAIDBExplorer", "GenAIDBExplorer.Console"),
            // When working directory already is console project
            ".",
            // When tasks set cwd to src/GenAIDBExplorer
            Path.Combine("GenAIDBExplorer.Console")
        };
        foreach (var root in candidateRoots.Distinct())
        {
            var appSettingsPath = Path.Combine(root, "appsettings.json");
            var envAppSettingsPath = Path.Combine(root, $"appsettings.{builder.Environment.EnvironmentName}.json");
            builder.Configuration
                .AddJsonFile(appSettingsPath, optional: true, reloadOnChange: true)
                .AddJsonFile(envAppSettingsPath, optional: true, reloadOnChange: true);
        }
        builder.Configuration.AddEnvironmentVariables();

        // Clear existing logging providers and configure new ones
        builder.Logging.ClearProviders();

        // Apply all logging configuration from appsettings.json FIRST
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        // Configure SimpleConsole provider
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });

        // Configure services
        ConfigureServices(builder.Services, builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure Azure persistence strategy options
        services.Configure<AzureBlobConfiguration>(
            configuration.GetSection(AzureBlobConfiguration.SectionName));
        services.Configure<CosmosDbConfiguration>(
            configuration.GetSection(CosmosDbConfiguration.SectionName));
        services.Configure<LocalDiskConfiguration>(
            configuration.GetSection(LocalDiskConfiguration.SectionName));

        // Configure caching options for Phase 5a: Basic Caching Foundation
        services.Configure<CacheOptions>(
            configuration.GetSection(CacheOptions.SectionName));

        // Configure security options for Phase 5b: Enhanced Security Features
        services.Configure<SecureJsonSerializerOptions>(
            configuration.GetSection("SecureJsonSerializer"));
        services.Configure<KeyVaultOptions>(
            configuration.GetSection("KeyVault"));

        // Register command handlers
        services.AddSingleton<InitProjectCommandHandler>();
        services.AddSingleton<DataDictionaryCommandHandler>();
        services.AddSingleton<EnrichModelCommandHandler>();
        services.AddSingleton<ExportModelCommandHandler>();
        services.AddSingleton<ExtractModelCommandHandler>();
        services.AddSingleton<QueryModelCommandHandler>();
        services.AddSingleton<ShowObjectCommandHandler>();
        services.AddSingleton<GenerateVectorsCommandHandler>();
        services.AddSingleton<ReconcileIndexCommandHandler>();

        // Register the Output service
        services.AddSingleton<IOutputService, OutputService>();

        // Register the Project service
        services.AddSingleton<IProject, Project>();

        // Register the database connection provider
        services.AddSingleton<IDatabaseConnectionProvider, SqlConnectionProvider>();

        // Register the database connection manager
        services.AddSingleton<IDatabaseConnectionManager, DatabaseConnectionManager>();

        // Register the SQL Query executor
        services.AddSingleton<ISqlQueryExecutor, SqlQueryExecutor>();

        // Register the Schema Repository
        services.AddSingleton<ISchemaRepository, SchemaRepository>();

        // Register the Semantic Model provider
        services.AddSingleton<ISemanticModelProvider>(provider =>
        {
            var project = provider.GetRequiredService<IProject>();
            var schemaRepository = provider.GetRequiredService<ISchemaRepository>();
            var logger = provider.GetRequiredService<ILogger<SemanticModelProvider>>();
            var semanticModelRepository = provider.GetRequiredService<ISemanticModelRepository>();

            return new SemanticModelProvider(project, schemaRepository, logger, semanticModelRepository);
        });

        // Register the Semantic Description provider
        services.AddSingleton<ISemanticDescriptionProvider, SemanticDescriptionProvider>();

        // Register the Data Dictionary provider
        services.AddSingleton<IDataDictionaryProvider, DataDictionaryProvider>();

        // Register the Semantic Kernel factory
        services.AddSingleton<ISemanticKernelFactory, SemanticKernelFactory>();

        // Vector indexing/search infrastructure
        services.AddSingleton<IVectorIndexPolicy, VectorIndexPolicy>();
        services.AddSingleton<IVectorInfrastructureFactory, VectorInfrastructureFactory>();
        services.AddSingleton<IVectorRecordMapper, VectorRecordMapper>();
        services.AddSingleton<IEmbeddingGenerator, SemanticKernelEmbeddingGenerator>();
        services.AddSingleton<IEntityKeyBuilder, EntityKeyBuilder>();
        services.AddSingleton<IVectorIndexWriter, SkInMemoryVectorIndexWriter>();
        services.AddSingleton<IVectorSearchService, SkInMemoryVectorSearchService>();
        services.AddSingleton<IVectorGenerationService, VectorGenerationService>(
            sp =>
            {
                // Inject current project settings instance into service
                var proj = sp.GetRequiredService<IProject>();

                return new VectorGenerationService(
                    proj.Settings,
                    sp.GetRequiredService<IVectorInfrastructureFactory>(),
                    sp.GetRequiredService<IVectorRecordMapper>(),
                    sp.GetRequiredService<IEmbeddingGenerator>(),
                    sp.GetRequiredService<IEntityKeyBuilder>(),
                    sp.GetRequiredService<IVectorIndexWriter>(),
                    sp.GetRequiredService<ISecureJsonSerializer>(),
                    sp.GetRequiredService<ILogger<VectorGenerationService>>(),
                    sp.GetRequiredService<IPerformanceMonitor>()
                );
            }
        );

        services.AddSingleton<IVectorOrchestrator, VectorOrchestrator>();

        // SK InMemory vector store for local/dev and tests
        services.AddSingleton<Microsoft.SemanticKernel.Connectors.InMemory.InMemoryVectorStore>();

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

            // Return null if Key Vault is not configured - this will be handled gracefully
            return null!;
        });

        // Register persistence strategies
        services.AddSingleton<ILocalDiskPersistenceStrategy, LocalDiskPersistenceStrategy>();
        
        // Register AzureBlobPersistenceStrategy using a factory that gets configuration from IProject
        services.AddSingleton<IAzureBlobPersistenceStrategy>(serviceProvider =>
        {
            var project = serviceProvider.GetRequiredService<IProject>();
            var logger = serviceProvider.GetRequiredService<ILogger<AzureBlobPersistenceStrategy>>();
            var secureJsonSerializer = serviceProvider.GetRequiredService<ISecureJsonSerializer>();
            var keyVaultProvider = serviceProvider.GetService<KeyVaultConfigurationProvider>();
            
            // Get Azure Blob configuration from project settings
            var azureBlobConfig = project.Settings.SemanticModelRepository?.AzureBlob;
            if (azureBlobConfig == null)
            {
                throw new InvalidOperationException("AzureBlob configuration is required when using AzureBlobPersistenceStrategy. Ensure SemanticModelRepository.AzureBlob is configured in project settings.json.");
            }
            
            return new AzureBlobPersistenceStrategy(azureBlobConfig, logger, secureJsonSerializer, keyVaultProvider);
        });
        
        // Register CosmosDbPersistenceStrategy using a factory that gets configuration from IProject
        services.AddSingleton<ICosmosDbPersistenceStrategy>(serviceProvider =>
        {
            var project = serviceProvider.GetRequiredService<IProject>();
            var logger = serviceProvider.GetRequiredService<ILogger<CosmosDbPersistenceStrategy>>();
            var secureJsonSerializer = serviceProvider.GetRequiredService<ISecureJsonSerializer>();
            var keyVaultProvider = serviceProvider.GetService<KeyVaultConfigurationProvider>();
            
            // Get Cosmos DB configuration from project settings
            var cosmosDbConfig = project.Settings.SemanticModelRepository?.CosmosDb;
            if (cosmosDbConfig == null)
            {
                throw new InvalidOperationException("CosmosDb configuration is required when using CosmosDbPersistenceStrategy. Ensure SemanticModelRepository.CosmosDb is configured in project settings.json.");
            }
            
            return new CosmosDbPersistenceStrategy(cosmosDbConfig, logger, secureJsonSerializer, keyVaultProvider);
        });

        // Register performance monitoring services (basic implementation, extensible for OpenTelemetry)
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();

        // Semantic Repository, Options Builders and persistence strategies
        services.AddSingleton<IPersistenceStrategyFactory, PersistenceStrategyFactory>();
        services.AddSingleton<ISemanticModelRepository, SemanticModelRepository>();

        services.AddTransient<ISemanticModelRepositoryOptionsBuilder>(
            provider =>
            {
                return SemanticModelRepositoryOptionsBuilder.Create();
            }
        );

        services.AddTransient<IPerformanceMonitoringOptionsBuilder>(
            provider =>
            {
                return PerformanceMonitoringOptionsBuilder.Create();
            }
        );
    }
}
