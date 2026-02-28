using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Core.ChatClients;
using GenAIDBExplorer.Core.Data.ConnectionManager;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.DataDictionary;
using GenAIDBExplorer.Core.Extensions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.PromptTemplates;
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
using GenAIDBExplorer.Core.SemanticModelQuery;
using GenAIDBExplorer.Core.Repository;
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
            Path.Combine("genai-database-explorer-service", "src", "GenAIDBExplorer.Console"),
            // When working directory already is console project
            ".",
            // When tasks set cwd to genai-database-explorer-service
            Path.Combine("src", "GenAIDBExplorer.Console")
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
        // Register shared Core services (repository, persistence, caching, security, performance monitoring)
        services.AddGenAIDBExplorerCoreServices(configuration);

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

        // Register AI service factories (replacing ISemanticKernelFactory)
        services.AddSingleton<IChatClientFactory, ChatClientFactory>();
        services.AddSingleton<IPromptTemplateParser, PromptTemplateParser>();
        services.AddSingleton<ILiquidTemplateRenderer, LiquidTemplateRenderer>();

        // Vector indexing/search infrastructure
        services.AddSingleton<IVectorIndexPolicy, VectorIndexPolicy>();
        services.AddSingleton<IVectorInfrastructureFactory, VectorInfrastructureFactory>();
        services.AddSingleton<IVectorRecordMapper, VectorRecordMapper>();
        services.AddSingleton<IEmbeddingGenerator, ChatClientEmbeddingGenerator>();
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
                    sp.GetRequiredService<ISemanticModelRepository>(),
                    sp.GetRequiredService<ILogger<VectorGenerationService>>(),
                    sp.GetRequiredService<IPerformanceMonitor>()
                );
            }
        );

        services.AddSingleton<IVectorOrchestrator, VectorOrchestrator>();

        // Query model services
        services.AddSingleton<ISemanticModelSearchService, SemanticModelSearchService>();
        services.AddSingleton<ISemanticModelQueryService, SemanticModelQueryService>();

        // SK InMemory vector store for local/dev and tests
        services.AddSingleton<Microsoft.SemanticKernel.Connectors.InMemory.InMemoryVectorStore>();
    }
}
