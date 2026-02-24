using GenAIDBExplorer.Api.Models;
using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Repository;

namespace GenAIDBExplorer.Api.Endpoints;

public static class TableEndpoints
{
    public static WebApplication MapTableEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tables")
            .WithTags("Tables");

        group.MapGet("/", ListTables)
            .WithName("ListTables")
            .WithDescription("List tables with pagination")
            .Produces<PaginatedResponse<EntitySummaryResponse>>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{schema}/{name}", GetTableDetail)
            .WithName("GetTableDetail")
            .WithDescription("Get table details including columns and indexes")
            .Produces<TableDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPatch("/{schema}/{name}", PatchTable)
            .WithName("PatchTable")
            .WithDescription("Update table descriptions")
            .Produces<TableDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPatch("/{schema}/{name}/columns/{columnName}", PatchTableColumn)
            .WithName("PatchTableColumn")
            .WithDescription("Update table column descriptions")
            .Produces<ColumnResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> ListTables(
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
            var tables = (await model.GetTablesAsync()).ToList();
            var totalCount = tables.Count;

            var items = tables
                .Skip(offset)
                .Take(limit)
                .Select(t => new EntitySummaryResponse(
                    t.Schema,
                    t.Name,
                    t.Description,
                    t.SemanticDescription,
                    t.NotUsed))
                .ToList();

            return Results.Ok(new PaginatedResponse<EntitySummaryResponse>(
                items, totalCount, offset, limit));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(TableEndpoints));
            logger.LogError(ex, "Failed to list tables");
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> GetTableDetail(
        ISemanticModelCacheService cacheService,
        ILoggerFactory loggerFactory,
        string schema,
        string name)
    {
        try
        {
            var model = await cacheService.GetModelAsync();
            var table = await model.FindTableAsync(schema, name);

            if (table is null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Table '{schema}.{name}' was not found in the semantic model.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
            }

            var columns = table.Columns.ToColumnResponses();
            var indexes = table.Indexes.ToIndexResponses();

            var response = new TableDetailResponse(
                table.Schema,
                table.Name,
                table.Description,
                table.SemanticDescription,
                table.SemanticDescriptionLastUpdate,
                table.Details,
                table.AdditionalInformation,
                table.NotUsed,
                table.NotUsedReason,
                columns,
                indexes);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(TableEndpoints));
            logger.LogError(ex, "Failed to get table detail for {Schema}.{Name}", schema, name);
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> PatchTable(
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
            var table = await model.FindTableAsync(schema, name);

            if (table is null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Table '{schema}.{name}' was not found in the semantic model.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
            }

            if (request.Description is not null)
            {
                table.Description = request.Description;
            }

            if (request.SemanticDescription is not null)
            {
                table.SetSemanticDescription(request.SemanticDescription);
            }

            if (request.NotUsed is not null)
            {
                table.NotUsed = request.NotUsed.Value;
            }

            if (request.NotUsedReason is not null)
            {
                table.NotUsedReason = request.NotUsedReason;
            }

            await repository.SaveChangesAsync(model, project.GetSemanticModelPath());

            var columns = table.Columns.ToColumnResponses();
            var indexes = table.Indexes.ToIndexResponses();

            var response = new TableDetailResponse(
                table.Schema,
                table.Name,
                table.Description,
                table.SemanticDescription,
                table.SemanticDescriptionLastUpdate,
                table.Details,
                table.AdditionalInformation,
                table.NotUsed,
                table.NotUsedReason,
                columns,
                indexes);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(TableEndpoints));
            logger.LogError(ex, "Failed to patch table {Schema}.{Name}", schema, name);
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }

    private static async Task<IResult> PatchTableColumn(
        ISemanticModelCacheService cacheService,
        ISemanticModelRepository repository,
        IProject project,
        ILoggerFactory loggerFactory,
        string schema,
        string name,
        string columnName,
        UpdateColumnDescriptionRequest request)
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
            var table = await model.FindTableAsync(schema, name);

            if (table is null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Table '{schema}.{name}' was not found in the semantic model.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
            }

            var column = table.Columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
            if (column is null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Column '{columnName}' was not found in table '{schema}.{name}'.",
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
            var logger = loggerFactory.CreateLogger(nameof(TableEndpoints));
            logger.LogError(ex, "Failed to patch column {ColumnName} in table {Schema}.{Name}", columnName, schema, name);
            return Results.Problem(
                title: "Service Unavailable",
                detail: "The semantic model is not currently available.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }
    }
}
