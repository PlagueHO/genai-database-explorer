using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Repository;

namespace GenAIDBExplorer.Api.Endpoints;

public static class ViewEndpoints
{
    public static WebApplication MapViewEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/views")
            .WithTags("Views");

        group.MapGet("/", ListViews)
            .WithName("ListViews")
            .WithDescription("List views with pagination")
            .Produces<PaginatedResponse<EntitySummaryResponse>>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{schema}/{name}", GetViewDetail)
            .WithName("GetViewDetail")
            .WithDescription("Get view details including columns and SQL definition")
            .Produces<ViewDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPatch("/{schema}/{name}", PatchView)
            .WithName("PatchView")
            .WithDescription("Update view descriptions")
            .Produces<ViewDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> ListViews(
        ISemanticModelCacheService cacheService,
        int offset = 0,
        int limit = 50)
    {
        if (offset < 0) offset = 0;
        if (limit < 1) limit = 1;
        if (limit > 200) limit = 200;

        try
        {
            var model = await cacheService.GetModelAsync();
            var views = (await model.GetViewsAsync()).ToList();
            var totalCount = views.Count;

            var items = views
                .Skip(offset)
                .Take(limit)
                .Select(v => new EntitySummaryResponse(
                    v.Schema,
                    v.Name,
                    v.Description,
                    v.SemanticDescription,
                    v.NotUsed))
                .ToList();

            return Results.Ok(new PaginatedResponse<EntitySummaryResponse>(
                items, totalCount, offset, limit));
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> GetViewDetail(
        ISemanticModelCacheService cacheService,
        string schema,
        string name)
    {
        try
        {
            var model = await cacheService.GetModelAsync();
            var view = await model.FindViewAsync(schema, name);

            if (view is null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"View '{schema}.{name}' was not found in the semantic model.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
            }

            var columns = view.Columns.Select(c => new ColumnResponse(
                c.Name, c.Type, c.Description, c.IsPrimaryKey, c.IsNullable,
                c.IsIdentity, c.IsComputed, c.IsXmlDocument, c.MaxLength,
                c.Precision, c.Scale, c.ReferencedTable, c.ReferencedColumn)).ToList();

            var response = new ViewDetailResponse(
                view.Schema,
                view.Name,
                view.Description,
                view.SemanticDescription,
                view.SemanticDescriptionLastUpdate,
                view.AdditionalInformation,
                view.Definition,
                view.NotUsed,
                view.NotUsedReason,
                columns);

            return Results.Ok(response);
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> PatchView(
        ISemanticModelCacheService cacheService,
        ISemanticModelRepository repository,
        IProject project,
        string schema,
        string name,
        UpdateEntityDescriptionRequest request)
    {
        if (request.Description is null && request.SemanticDescription is null)
        {
            return Results.Problem(
                title: "Bad Request",
                detail: "At least one of Description or SemanticDescription must be provided.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");
        }

        try
        {
            var model = await cacheService.GetModelAsync();
            var view = await model.FindViewAsync(schema, name);

            if (view is null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"View '{schema}.{name}' was not found in the semantic model.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
            }

            if (request.Description is not null)
            {
                view.Description = request.Description;
            }

            if (request.SemanticDescription is not null)
            {
                view.SetSemanticDescription(request.SemanticDescription);
            }

            var semanticModelDirectory = project.Settings.SemanticModelRepository?.LocalDisk?.Directory
                ?? throw new InvalidOperationException("LocalDisk persistence strategy is configured but no directory is specified in SemanticModelRepository.LocalDisk.Directory.");
            var modelPath = new DirectoryInfo(
                Path.Combine(project.ProjectDirectory.FullName, semanticModelDirectory));
            await repository.SaveChangesAsync(model, modelPath);

            var columns = view.Columns.Select(c => new ColumnResponse(
                c.Name, c.Type, c.Description, c.IsPrimaryKey, c.IsNullable,
                c.IsIdentity, c.IsComputed, c.IsXmlDocument, c.MaxLength,
                c.Precision, c.Scale, c.ReferencedTable, c.ReferencedColumn)).ToList();

            var response = new ViewDetailResponse(
                view.Schema,
                view.Name,
                view.Description,
                view.SemanticDescription,
                view.SemanticDescriptionLastUpdate,
                view.AdditionalInformation,
                view.Definition,
                view.NotUsed,
                view.NotUsedReason,
                columns);

            return Results.Ok(response);
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }
}
