using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Trello;

var builder = Host.CreateApplicationBuilder(args);

// ── Jira credentials ──────────────────────────────────────────────────────────
var jiraBaseUrl  = builder.Configuration["JIRA_BASE_URL"]  ?? string.Empty;
var jiraEmail    = builder.Configuration["JIRA_EMAIL"]     ?? string.Empty;
var jiraApiToken = builder.Configuration["JIRA_API_TOKEN"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(jiraBaseUrl) ||
    string.IsNullOrWhiteSpace(jiraEmail)   ||
    string.IsNullOrWhiteSpace(jiraApiToken))
{
    throw new InvalidOperationException(
        "Missing Jira credentials. Set JIRA_BASE_URL, JIRA_EMAIL, and JIRA_API_TOKEN.");
}

// ── Trello credentials ────────────────────────────────────────────────────────
var trelloApiKey = builder.Configuration["TRELLO_API_KEY"] ?? string.Empty;
var trelloToken  = builder.Configuration["TRELLO_TOKEN"]   ?? string.Empty;

if (string.IsNullOrWhiteSpace(trelloApiKey) || string.IsNullOrWhiteSpace(trelloToken))
{
    throw new InvalidOperationException(
        "Missing Trello credentials. Set TRELLO_API_KEY and TRELLO_TOKEN.");
}

// ── GitHub credentials ────────────────────────────────────────────────────────
var githubToken = builder.Configuration["GITHUB_TOKEN"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(githubToken))
{
    throw new InvalidOperationException(
        "Missing GitHub credentials. Set GITHUB_TOKEN.");
}

// ── HTTP clients ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<IJiraClient, JiraClient>(client =>
{
    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{jiraEmail}:{jiraApiToken}"));
    client.BaseAddress = new Uri(jiraBaseUrl.TrimEnd('/') + "/rest/api/3/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", credentials);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient<ITrelloClient, TrelloClient>(client =>
{
    client.BaseAddress = new Uri($"https://api.trello.com/1/");
    client.DefaultRequestHeaders.Add("Authorization",
        $"OAuth oauth_consumer_key=\"{trelloApiKey}\", oauth_token=\"{trelloToken}\"");
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient<IGitHubClient, GitHubClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", githubToken);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ProjectManagement-MCP/1.0");
});

// ── MCP server ────────────────────────────────────────────────────────────────
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
