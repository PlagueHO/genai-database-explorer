using System.Text.Json;
using GenAIDBExplorer.Api.Endpoints;
using GenAIDBExplorer.Api.Health;
using GenAIDBExplorer.Api.Services;
using GenAIDBExplorer.Core.Extensions;
using GenAIDBExplorer.Core.Models.Project;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Register shared Core services (repository, persistence, caching, security, performance monitoring)
builder.Services.AddGenAIDBExplorerCoreServices(builder.Configuration);

// Register API-layer services
builder.Services.AddSingleton<ISemanticModelCacheService, SemanticModelCacheService>();

// Register custom health check for semantic model
builder.Services.AddHealthChecks()
    .AddCheck<SemanticModelHealthCheck>("semantic-model");

// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Configure CORS
builder.Services.AddCors();

// Add OpenAPI document generation
builder.Services.AddOpenApi();

var app = builder.Build();

// Load project configuration at startup
var projectPath = builder.Configuration["GenAIDBExplorer:ProjectPath"];
if (string.IsNullOrWhiteSpace(projectPath))
{
    throw new InvalidOperationException(
        "GenAIDBExplorer:ProjectPath must be configured in appsettings.json or environment variables.");
}

var project = app.Services.GetRequiredService<IProject>();
project.LoadProjectConfiguration(new DirectoryInfo(projectPath));

// Pre-load the semantic model into cache (non-fatal — API starts degraded if model unavailable)
var cacheService = app.Services.GetRequiredService<ISemanticModelCacheService>();
try
{
    await cacheService.GetModelAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to pre-load semantic model at startup. The API will start in degraded mode — endpoints will return 503 until the model is loaded via POST /api/model/reload");
}

// Configure middleware pipeline

// Global exception handler producing RFC 9457 Problem Details
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
        logger.LogError(exceptionFeature?.Error, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred while processing the request.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply CORS policy
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Contains("*"))
{
    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}
else if (allowedOrigins.Length > 0)
{
    app.UseCors(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader());
}

// Map Aspire default health endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Map API endpoints
app.MapModelEndpoints();
app.MapTableEndpoints();
app.MapViewEndpoints();
app.MapStoredProcedureEndpoints();
app.MapProjectEndpoints();

app.Run();

// Enable WebApplicationFactory access for integration tests
public partial class Program { }
