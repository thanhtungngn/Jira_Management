using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.OpenApi.Models;
using ProjectManagement.Core.Jira;

var builder = WebApplication.CreateBuilder(args);

// ── Jira credentials ──────────────────────────────────────────────────────────
var baseUrl  = builder.Configuration["JIRA_BASE_URL"]  ?? string.Empty;
var email    = builder.Configuration["JIRA_EMAIL"]     ?? string.Empty;
var apiToken = builder.Configuration["JIRA_API_TOKEN"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(baseUrl) ||
    string.IsNullOrWhiteSpace(email)   ||
    string.IsNullOrWhiteSpace(apiToken))
{
    throw new InvalidOperationException(
        "Missing Jira credentials. " +
        "Set JIRA_BASE_URL, JIRA_EMAIL, and JIRA_API_TOKEN " +
        "in appsettings.json or as environment variables.");
}

// ── Services ──────────────────────────────────────────────────────────────────
var appVersion = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion ?? "1.0.0";

builder.Services.AddControllers();

builder.Services.AddHttpClient<IJiraClient, JiraClient>(client =>
{
    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{apiToken}"));
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/rest/api/3/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", credentials);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Project Management API",
        Version     = $"v{appVersion}",
        Description = "REST API for managing Jira projects and issues.",
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
