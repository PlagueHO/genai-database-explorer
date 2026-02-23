using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Api.Endpoints;

public static class ProjectEndpoints
{
    public static WebApplication MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/project")
            .WithTags("Project");

        group.MapGet("/", GetProjectInfo)
            .WithName("GetProjectInfo")
            .WithDescription("Get project configuration info")
            .Produces<ProjectInfoResponse>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> GetProjectInfo(
        IProject project,
        ISemanticModelCacheService cacheService)
    {
        try
        {
            var isLoaded = cacheService.IsLoaded;
            var modelName = string.Empty;
            var modelSource = string.Empty;

            if (isLoaded)
            {
                var model = await cacheService.GetModelAsync();
                modelName = model.Name;
                modelSource = model.Source;
            }

            var persistenceStrategy = "LocalDisk";
            var settings = project.Settings?.SemanticModelRepository;
            if (settings?.CosmosDb != null)
            {
                persistenceStrategy = "CosmosDB";
            }
            else if (settings?.AzureBlob != null)
            {
                persistenceStrategy = "AzureBlob";
            }

            var response = new ProjectInfoResponse(
                project.ProjectDirectory.FullName,
                modelName,
                modelSource,
                persistenceStrategy,
                isLoaded);

            return Results.Ok(response);
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "Service Unavailable",
                detail: "Unable to retrieve project information.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }
}
