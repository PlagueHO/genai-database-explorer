using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Api.Services;

namespace GenAIDBExplorer.Api.Endpoints;

public static class ModelEndpoints
{
    public static WebApplication MapModelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/model")
            .WithTags("Model");

        group.MapGet("/", GetModelSummary)
            .WithName("GetModelSummary")
            .WithDescription("Retrieve semantic model summary")
            .Produces<SemanticModelSummaryResponse>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/reload", ReloadModel)
            .WithName("ReloadModel")
            .WithDescription("Reload model from persistence")
            .Produces<SemanticModelSummaryResponse>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> GetModelSummary(
        ISemanticModelCacheService cacheService,
        ILoggerFactory loggerFactory)
    {
        try
        {
            var model = await cacheService.GetModelAsync();
            var response = new SemanticModelSummaryResponse(
                model.Name,
                model.Source,
                model.Description,
                model.Tables.Count,
                model.Views.Count,
                model.StoredProcedures.Count);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ModelEndpoints));
            logger.LogError(ex, "Failed to retrieve semantic model summary");
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> ReloadModel(
        ISemanticModelCacheService cacheService,
        ILoggerFactory loggerFactory)
    {
        try
        {
            var model = await cacheService.ReloadModelAsync();
            var response = new SemanticModelSummaryResponse(
                model.Name,
                model.Source,
                model.Description,
                model.Tables.Count,
                model.Views.Count,
                model.StoredProcedures.Count);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ModelEndpoints));
            logger.LogError(ex, "Failed to reload semantic model");
            return Results.Problem(
                title: "Service Unavailable",
                detail: "Failed to reload the semantic model.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }
}
