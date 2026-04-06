using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectManagement.Core;

var builder = Host.CreateApplicationBuilder(args);

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
