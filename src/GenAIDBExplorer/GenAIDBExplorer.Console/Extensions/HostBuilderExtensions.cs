using GenAIDBExplorer.Console.Services;
using GenAIDBExplorer.Console.CommandHandlers;
using GenAIDBExplorer.Core.Data.ConnectionManager;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.DataDictionary;
using GenAIDBExplorer.Core.KernelMemory;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.SemanticKernel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.SemanticProviders;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.Repository.Configuration;
using GenAIDBExplorer.Core.Repository.Caching;
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
    /// Configures the host builder with the necessary services and configurations.
    /// </summary>
    /// <param name="builder">The <see cref="IHostBuilder"/> instance.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The configured <see cref="IHostBuilder"/> instance.</returns>
    public static IHostBuilder ConfigureHost(this IHostBuilder builder, string[] args)
    {
        return builder
            .ConfigureAppConfiguration((context, config) =>
            {
                config
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureLogging((context, config) =>
            {
                config
                    .ClearProviders()
                    .AddConfiguration(context.Configuration.GetSection("Logging"))
                    .AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                    });
            })
            .ConfigureServices((context, services) =>
            {
                // Configure Azure persistence strategy options
                services.Configure<AzureBlobStorageConfiguration>(
                    context.Configuration.GetSection("AzureBlobStorage"));
                services.Configure<CosmosDbConfiguration>(
                    context.Configuration.GetSection("CosmosDb"));

                // Configure caching options for Phase 5a: Basic Caching Foundation
                services.Configure<CacheOptions>(
                    context.Configuration.GetSection(CacheOptions.SectionName));

                // Configure security options for Phase 5b: Enhanced Security Features
                services.Configure<SecureJsonSerializerOptions>(
                    context.Configuration.GetSection("SecureJsonSerializer"));
                services.Configure<KeyVaultOptions>(
                    context.Configuration.GetSection("KeyVault"));

                // Register command handlers
                services.AddSingleton<InitProjectCommandHandler>();
                services.AddSingleton<DataDictionaryCommandHandler>();
                services.AddSingleton<EnrichModelCommandHandler>();
                services.AddSingleton<ExportModelCommandHandler>();
                services.AddSingleton<ExtractModelCommandHandler>();
                services.AddSingleton<QueryModelCommandHandler>();
                services.AddSingleton<ShowObjectCommandHandler>();

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
                services.AddSingleton<ISemanticModelProvider, SemanticModelProvider>();

                // Register the Semantic Description provider
                services.AddSingleton<ISemanticDescriptionProvider, SemanticDescriptionProvider>();

                // Register the Data Dictionary provider
                services.AddSingleton<IDataDictionaryProvider, DataDictionaryProvider>();

                // Register the Semantic Kernel factory
                services.AddSingleton<ISemanticKernelFactory, SemanticKernelFactory>();

                // Register the Kernel Memory factory
                services.AddSingleton(provider =>
                {
                    var project = provider.GetRequiredService<IProject>();
                    return new KernelMemoryFactory().CreateKernelMemory(project)(provider);
                });

                // Register caching services for Phase 5a: Basic Caching Foundation
                services.AddMemoryCache();
                services.AddSingleton<ISemanticModelCache, MemorySemanticModelCache>();

                // Register security services for Phase 5b: Enhanced Security Features
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
                services.AddSingleton<IAzureBlobPersistenceStrategy, AzureBlobPersistenceStrategy>();
                services.AddSingleton<ICosmosPersistenceStrategy, CosmosPersistenceStrategy>();

                // Register persistence strategy factory and repository
                services.AddSingleton<IPersistenceStrategyFactory, PersistenceStrategyFactory>();
                services.AddSingleton<ISemanticModelRepository, SemanticModelRepository>();
            })
            .UseConsoleLifetime();
    }
}
