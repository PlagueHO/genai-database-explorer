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
        // Configure additional JSON files and environment variables
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Clear existing logging providers and configure new ones
        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });

        // Explicitly set minimum level from configuration to ensure it takes effect
        var defaultLogLevel = builder.Configuration["Logging:LogLevel:Default"];
        if (!string.IsNullOrEmpty(defaultLogLevel) && Enum.TryParse<LogLevel>(defaultLogLevel, true, out var logLevel))
        {
            builder.Logging.SetMinimumLevel(logLevel);
        }

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
        services.Configure<AzureBlobStorageConfiguration>(
            configuration.GetSection(AzureBlobStorageConfiguration.SectionName));
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

        // Register performance monitoring services (basic implementation, extensible for OpenTelemetry)
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();

        // SEmantic Repository Repository, Options Builders and persistence strategies
        services.AddSingleton<IPersistenceStrategyFactory, PersistenceStrategyFactory>();
        services.AddSingleton<ISemanticModelRepository, SemanticModelRepository>();
        services.AddTransient<ISemanticModelRepositoryOptionsBuilder>(provider =>
            SemanticModelRepositoryOptionsBuilder.Create());
        services.AddTransient<IPerformanceMonitoringOptionsBuilder>(provider =>
            PerformanceMonitoringOptionsBuilder.Create());
    }

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

                // Ensure Debug level is explicitly set if configured
                var defaultLogLevel = context.Configuration["Logging:LogLevel:Default"];
                if (!string.IsNullOrEmpty(defaultLogLevel) && Enum.TryParse<LogLevel>(defaultLogLevel, true, out var logLevel))
                {
                    config.SetMinimumLevel(logLevel);
                }
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services, context.Configuration);
            })
            .UseConsoleLifetime();
    }
}
