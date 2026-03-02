using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Core.SemanticModelQuery;

namespace GenAIDBExplorer.Api.Endpoints;

/// <summary>
/// Extension methods to register the semantic model search endpoints.
/// </summary>
public static class SearchEndpoints
{
    private static readonly string[] ValidEntityTypes = ["table", "view", "storedProcedure"];
    private const int DefaultTopK = 10;
    private const int MaxTopK = 10;

    /// <summary>
    /// Maps the search endpoints to the web application.
    /// </summary>
    public static WebApplication MapSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/search")
            .WithTags("Search");

        group.MapPost("/", SearchEntities)
            .WithName("SearchEntities")
            .WithDescription("Search semantic model entities using natural language")
            .Produces<SearchResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> SearchEntities(
        ISemanticModelSearchService searchService,
        ILoggerFactory loggerFactory,
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        // Validate query
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.Problem(
                title: "Bad Request",
                detail: "Query must not be empty.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");
        }

        if (request.Query.Length > 2000)
        {
            return Results.Problem(
                title: "Bad Request",
                detail: "Query must not exceed 2000 characters.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");
        }

        // Validate and clamp limit
        var limit = request.Limit ?? DefaultTopK;
        if (limit < 1)
        {
            return Results.Problem(
                title: "Bad Request",
                detail: "Limit must be at least 1.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");
        }

        limit = Math.Min(limit, MaxTopK);

        // Validate entity types
        if (request.EntityTypes is not null && request.EntityTypes.Count > 0)
        {
            var invalidTypes = request.EntityTypes
                .Where(et => !ValidEntityTypes.Contains(et, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (invalidTypes.Count > 0)
            {
                return Results.Problem(
                    title: "Bad Request",
                    detail: $"Invalid entity type(s). Valid values are: {string.Join(", ", ValidEntityTypes)}.",
                    statusCode: StatusCodes.Status400BadRequest,
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");
            }
        }

        try
        {
            var results = await searchService.SearchAsync(
                request.Query,
                limit,
                request.EntityTypes,
                cancellationToken);

            var response = new SearchResponse(
                results.Select(r => new SearchResultResponse(
                    r.EntityType,
                    r.SchemaName,
                    r.EntityName,
                    r.Content,
                    r.Score))
                .ToList(),
                results.Count);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(SearchEndpoints));
            logger.LogError(ex, "Failed to search semantic model entities");
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model search service is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }
}
