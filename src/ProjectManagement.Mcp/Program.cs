using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectManagement.Core;

var builder = Host.CreateApplicationBuilder(args);

// appsettings.json and environment variables are loaded automatically
// by Host.CreateApplicationBuilder. AddEnvironmentVariables() is also
// included by default, so no manual configuration setup is needed.

// Register all three clients via IConfiguration-based options
builder.Services
    .AddJiraClient(builder.Configuration)
    .AddTrelloClient(builder.Configuration)
    .AddGitHubClient(builder.Configuration);

// ── MCP server ────────────────────────────────────────────────────────────────
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
