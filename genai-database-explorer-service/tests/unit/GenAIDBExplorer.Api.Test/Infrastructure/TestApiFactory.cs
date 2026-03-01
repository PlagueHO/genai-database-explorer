using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository;
using GenAIDBExplorer.Core.SemanticModelQuery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace GenAIDBExplorer.Api.Test.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that replaces core services with mocks for endpoint testing.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    public Mock<ISemanticModelCacheService> MockCacheService { get; } = new();
    public Mock<IProject> MockProject { get; } = new();
    public Mock<ISemanticModelRepository> MockRepository { get; } = new();
    public Mock<ISemanticModelSearchService> MockSearchService { get; } = new();

    public TestApiFactory()
    {
        // Set up defaults so app startup succeeds
        var defaultModel = TestData.CreateTestModel();
        MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(defaultModel);
        MockCacheService.Setup(c => c.IsLoaded).Returns(true);

        MockProject.Setup(p => p.ProjectDirectory)
            .Returns(new DirectoryInfo(Path.GetTempPath()));
        MockProject.Setup(p => p.Settings).Returns(new ProjectSettings
        {
            Database = new DatabaseSettings(),
            DataDictionary = new DataDictionarySettings(),
            SemanticModel = new SemanticModelSettings(),
            MicrosoftFoundry = new MicrosoftFoundrySettings(),
            SemanticModelRepository = new SemanticModelRepositorySettings
            {
                LocalDisk = new LocalDiskConfiguration { Directory = "semantic-model" }
            },
            VectorIndex = new VectorIndexSettings()
        });
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("GenAIDBExplorer:ProjectPath", Path.GetTempPath());

        builder.ConfigureServices(services =>
        {
            // Replace core services with mocks
            services.RemoveAll<IProject>();
            services.AddSingleton(MockProject.Object);

            services.RemoveAll<ISemanticModelCacheService>();
            services.AddSingleton(MockCacheService.Object);

            services.RemoveAll<ISemanticModelRepository>();
            services.AddSingleton(MockRepository.Object);

            services.RemoveAll<ISemanticModelSearchService>();
            services.AddSingleton(MockSearchService.Object);
        });
    }

    /// <summary>
    /// Resets all mock setups except for the defaults needed for app startup.
    /// </summary>
    public void ResetMocks()
    {
        MockCacheService.Reset();
        MockRepository.Reset();
        MockSearchService.Reset();

        // Re-establish default so future requests don't fail unexpectedly
        var defaultModel = TestData.CreateTestModel();
        MockCacheService.Setup(c => c.GetModelAsync()).ReturnsAsync(defaultModel);
        MockCacheService.Setup(c => c.IsLoaded).Returns(true);
    }
}
