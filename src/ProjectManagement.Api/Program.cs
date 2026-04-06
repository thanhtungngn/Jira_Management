using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.OpenApi;
using ProjectManagement.Core;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// Register all three API clients via IConfiguration-based options
builder.Services
    .AddJiraClient(builder.Configuration)
    .AddTrelloClient(builder.Configuration)
    .AddGitHubClient(builder.Configuration);

var appVersion = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion ?? "1.0.0";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title       = "Project Management API",
        Version     = $"v{appVersion}",
        Description = "REST API for managing Jira, Trello, and GitHub resources.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", $"Project Management API v{appVersion}"));

app.UseExceptionHandler(exApp => exApp.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    var logger  = context.RequestServices.GetRequiredService<ILogger<Program>>();

    if (feature?.Error is HttpRequestException httpEx)
    {
        logger.LogWarning(httpEx, "API error on {Method} {Path}",
            context.Request.Method, context.Request.Path);
        context.Response.StatusCode  = StatusCodes.Status502BadGateway;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            title  = "API Error",
            status = StatusCodes.Status502BadGateway,
            detail = httpEx.Message,
        });
        return;
    }

    logger.LogError(feature?.Error, "Unhandled exception on {Method} {Path}",
        context.Request.Method, context.Request.Path);
    context.Response.StatusCode  = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new
    {
        title  = "Internal Server Error",
        status = StatusCodes.Status500InternalServerError,
        detail = feature?.Error?.Message,
    });
}));

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make the implicit Program class accessible to the integration test project
public partial class Program { }
