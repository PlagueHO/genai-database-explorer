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

        group.MapPatch("/{schema}/{name}/columns/{columnName}", PatchViewColumn)
            .WithName("PatchViewColumn")
            .WithDescription("Update view column descriptions")
            .Produces<ColumnResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> ListViews(
        ISemanticModelCacheService cacheService,
        ILoggerFactory loggerFactory,
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
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ViewEndpoints));
            logger.LogError(ex, "Failed to list views");
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> GetViewDetail(
        ISemanticModelCacheService cacheService,
        ILoggerFactory loggerFactory,
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

            var columns = view.Columns.ToColumnResponses();

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
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ViewEndpoints));
            logger.LogError(ex, "Failed to get view detail for {Schema}.{Name}", schema, name);
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
        ILoggerFactory loggerFactory,
        string schema,
        string name,
        UpdateEntityDescriptionRequest request)
    {
        if (request.Description is null && request.SemanticDescription is null && request.NotUsed is null && request.NotUsedReason is null)
        {
            return Results.Problem(
                title: "Bad Request",
                detail: "At least one of Description, SemanticDescription, NotUsed, or NotUsedReason must be provided.",
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

            if (request.NotUsed is not null)
            {
                view.NotUsed = request.NotUsed.Value;
            }

            if (request.NotUsedReason is not null)
            {
                view.NotUsedReason = request.NotUsedReason;
            }

            await repository.SaveChangesAsync(model, project.GetSemanticModelPath());

            var columns = view.Columns.ToColumnResponses();

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
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ViewEndpoints));
            logger.LogError(ex, "Failed to patch view {Schema}.{Name}", schema, name);
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> PatchViewColumn(
        string schema,
        string name,
        string columnName,
        UpdateColumnDescriptionRequest request,
        ISemanticModelCacheService cacheService,
        ISemanticModelRepository repository,
        IProject project,
        ILoggerFactory loggerFactory)
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

            var column = view.Columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (column is null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Column '{columnName}' was not found in view '{schema}.{name}'.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
            }

            if (request.Description is not null)
            {
                column.Description = request.Description;
            }

            if (request.SemanticDescription is not null)
            {
                column.SetSemanticDescription(request.SemanticDescription);
            }

            await repository.SaveChangesAsync(model, project.GetSemanticModelPath());

            var response = new ColumnResponse(
                column.Name, column.Type, column.Description, column.IsPrimaryKey,
                column.IsNullable, column.IsIdentity, column.IsComputed, column.IsXmlDocument,
                column.MaxLength, column.Precision, column.Scale,
                column.ReferencedTable, column.ReferencedColumn);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ViewEndpoints));
            logger.LogError(ex, "Failed to patch column {ColumnName} on view {Schema}.{Name}", columnName, schema, name);
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }
}
