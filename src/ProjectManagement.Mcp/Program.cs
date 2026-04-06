using ProjectManagement.Core;

var builder = WebApplication.CreateBuilder(args);

// appsettings.json and environment variables are loaded automatically
// by WebApplication.CreateBuilder.

// Register all three clients via IConfiguration-based options
builder.Services
    .AddJiraClient(builder.Configuration)
    .AddTrelloClient(builder.Configuration)
    .AddGitHubClient(builder.Configuration);

// ── Keep-alive: ping /health every 10 min to prevent Render free-tier sleep ──
builder.Services.AddHttpClient();
builder.Services.AddHostedService<KeepAliveService>();

// ── MCP server ────────────────────────────────────────────────────────────────
// Stateless mode: no Mcp-Session-Id required — each request is self-contained.
// Required when deployed behind a reverse proxy (Render, etc.) and when the
// client (VS Code Copilot) does not manage session headers.
builder.Services
    .AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());
app.MapMcp("/mcp");

await app.RunAsync();

// ── Keep-alive background service ─────────────────────────────────────────────
sealed class KeepAliveService(IHttpClientFactory factory, ILogger<KeepAliveService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var client = factory.CreateClient();
                await client.GetAsync("http://localhost/health", stoppingToken);
                logger.LogDebug("Keep-alive ping sent");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Keep-alive ping failed");
            }
        }
    }
}
