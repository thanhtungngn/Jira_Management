using ProjectManagement.Core;

var builder = WebApplication.CreateBuilder(args);

// appsettings.json and environment variables are loaded automatically
// by WebApplication.CreateBuilder.

// Register all three clients via IConfiguration-based options
builder.Services
    .AddJiraClient(builder.Configuration)
    .AddTrelloClient(builder.Configuration)
    .AddGitHubClient(builder.Configuration);

// ── MCP server ────────────────────────────────────────────────────────────────
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());
app.MapMcp("/mcp");

await app.RunAsync();
