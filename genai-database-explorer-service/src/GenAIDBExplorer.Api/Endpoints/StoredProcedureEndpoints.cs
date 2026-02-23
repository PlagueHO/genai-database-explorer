using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Repository;

namespace GenAIDBExplorer.Api.Endpoints;

public static class StoredProcedureEndpoints
{
    public static WebApplication MapStoredProcedureEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/stored-procedures")
            .WithTags("StoredProcedures");

        group.MapGet("/", ListStoredProcedures)
            .WithName("ListStoredProcedures")
            .WithDescription("List stored procedures with pagination")
            .Produces<PaginatedResponse<EntitySummaryResponse>>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{schema}/{name}", GetStoredProcedureDetail)
            .WithName("GetStoredProcedureDetail")
            .WithDescription("Get stored procedure details including parameters and SQL definition")
            .Produces<StoredProcedureDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPatch("/{schema}/{name}", PatchStoredProcedure)
            .WithName("PatchStoredProcedure")
            .WithDescription("Update stored procedure descriptions")
            .Produces<StoredProcedureDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> ListStoredProcedures(
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
            var storedProcedures = (await model.GetStoredProceduresAsync()).ToList();
            var totalCount = storedProcedures.Count;

            var items = storedProcedures
                .Skip(offset)
                .Take(limit)
                .Select(sp => new EntitySummaryResponse(
                    sp.Schema,
                    sp.Name,
                    sp.Description,
                    sp.SemanticDescription,
                    sp.NotUsed))
                .ToList();

            return Results.Ok(new PaginatedResponse<EntitySummaryResponse>(
                items, totalCount, offset, limit));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(StoredProcedureEndpoints));
            logger.LogError(ex, "Failed to list stored procedures");
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> GetStoredProcedureDetail(
        ISemanticModelCacheService cacheService,
        ILoggerFactory loggerFactory,
        string schema,
        string name)
    {
        try
        {
            var model = await cacheService.GetModelAsync();
            var storedProcedure = await model.FindStoredProcedureAsync(schema, name);

            if (storedProcedure is null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Stored procedure '{schema}.{name}' was not found in the semantic model.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
            }

            var response = new StoredProcedureDetailResponse(
                storedProcedure.Schema,
                storedProcedure.Name,
                storedProcedure.Description,
                storedProcedure.SemanticDescription,
                storedProcedure.SemanticDescriptionLastUpdate,
                storedProcedure.AdditionalInformation,
                storedProcedure.Parameters,
                storedProcedure.Definition,
                storedProcedure.NotUsed,
                storedProcedure.NotUsedReason);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(StoredProcedureEndpoints));
            logger.LogError(ex, "Failed to get stored procedure detail for {Schema}.{Name}", schema, name);
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> PatchStoredProcedure(
        ISemanticModelCacheService cacheService,
        ISemanticModelRepository repository,
        IProject project,
        ILoggerFactory loggerFactory,
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
            var storedProcedure = await model.FindStoredProcedureAsync(schema, name);

            if (storedProcedure is null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Stored procedure '{schema}.{name}' was not found in the semantic model.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
            }

            if (request.Description is not null)
            {
                storedProcedure.Description = request.Description;
            }

            if (request.SemanticDescription is not null)
            {
                storedProcedure.SetSemanticDescription(request.SemanticDescription);
            }

            await repository.SaveChangesAsync(model, project.GetSemanticModelPath());

            var response = new StoredProcedureDetailResponse(
                storedProcedure.Schema,
                storedProcedure.Name,
                storedProcedure.Description,
                storedProcedure.SemanticDescription,
                storedProcedure.SemanticDescriptionLastUpdate,
                storedProcedure.AdditionalInformation,
                storedProcedure.Parameters,
                storedProcedure.Definition,
                storedProcedure.NotUsed,
                storedProcedure.NotUsedReason);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(StoredProcedureEndpoints));
            logger.LogError(ex, "Failed to patch stored procedure {Schema}.{Name}", schema, name);
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }
}
