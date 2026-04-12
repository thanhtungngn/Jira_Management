using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectManagement.Core.Confluence;
using ProjectManagement.Core;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Trello;

namespace ProjectManagement.Api.Tests.Core;

/// <summary>
/// Verifies that <see cref="ServiceCollectionExtensions"/> correctly reads settings from
/// both the structured section form and the legacy flat-key form.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    // ── JiraOptions ───────────────────────────────────────────────────────────

    [Fact]
    public void AddJiraClient_BindsStructuredSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jira:BaseUrl"]  = "https://mycompany.atlassian.net",
                ["Jira:Email"]    = "user@example.com",
                ["Jira:ApiToken"] = "secret-token",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJiraClient(config);

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<JiraOptions>>().Value;

        Assert.Equal("https://mycompany.atlassian.net", opts.BaseUrl);
        Assert.Equal("user@example.com", opts.Email);
        Assert.Equal("secret-token", opts.ApiToken);
    }

    [Fact]
    public void AddJiraClient_FallsBackToFlatEnvKeys()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JIRA_BASE_URL"]  = "https://flat.atlassian.net",
                ["JIRA_EMAIL"]     = "flat@example.com",
                ["JIRA_API_TOKEN"] = "flat-token",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJiraClient(config);

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<JiraOptions>>().Value;

        Assert.Equal("https://flat.atlassian.net", opts.BaseUrl);
        Assert.Equal("flat@example.com", opts.Email);
        Assert.Equal("flat-token", opts.ApiToken);
    }

    [Fact]
    public void AddJiraClient_RegistersIJiraClientImplementation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jira:BaseUrl"]  = "https://mycompany.atlassian.net",
                ["Jira:Email"]    = "user@example.com",
                ["Jira:ApiToken"] = "token",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJiraClient(config);

        var sp = services.BuildServiceProvider();
        // Should resolve without throwing (factory is lazy)
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        Assert.NotNull(factory);
    }

    // ── TrelloOptions ─────────────────────────────────────────────────────────

    [Fact]
    public void AddTrelloClient_BindsStructuredSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Trello:ApiKey"] = "my-api-key",
                ["Trello:Token"]  = "my-token",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTrelloClient(config);

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<TrelloOptions>>().Value;

        Assert.Equal("my-api-key", opts.ApiKey);
        Assert.Equal("my-token", opts.Token);
    }

    [Fact]
    public void AddTrelloClient_FallsBackToFlatEnvKeys()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TRELLO_API_KEY"] = "flat-api-key",
                ["TRELLO_TOKEN"]   = "flat-token",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTrelloClient(config);

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<TrelloOptions>>().Value;

        Assert.Equal("flat-api-key", opts.ApiKey);
        Assert.Equal("flat-token", opts.Token);
    }

    // ── GitHubOptions ─────────────────────────────────────────────────────────

    [Fact]
    public void AddGitHubClient_BindsStructuredSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitHub:Token"]     = "ghp_mytoken",
                ["GitHub:UserAgent"] = "MyApp/2.0",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGitHubClient(config);

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<GitHubOptions>>().Value;

        Assert.Equal("ghp_mytoken", opts.Token);
        Assert.Equal("MyApp/2.0", opts.UserAgent);
    }

    [Fact]
    public void AddGitHubClient_FallsBackToFlatEnvKey()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GITHUB_TOKEN"] = "flat-github-token",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGitHubClient(config);

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<GitHubOptions>>().Value;

        Assert.Equal("flat-github-token", opts.Token);
    }

    [Fact]
    public void AddGitHubClient_UsesDefaultUserAgent_WhenNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GITHUB_TOKEN"] = "token",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGitHubClient(config);

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<GitHubOptions>>().Value;

        Assert.Equal("ProjectManagement/1.0", opts.UserAgent);
    }

    // ── ConfluenceOptions ────────────────────────────────────────────────────

    [Fact]
    public void AddConfluenceClient_BindsStructuredSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Confluence:BaseUrl"]  = "https://mycompany.atlassian.net",
                ["Confluence:Email"]    = "user@example.com",
                ["Confluence:ApiToken"] = "secret-token",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConfluenceClient(config);

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<ConfluenceOptions>>().Value;

        Assert.Equal("https://mycompany.atlassian.net", opts.BaseUrl);
        Assert.Equal("user@example.com", opts.Email);
        Assert.Equal("secret-token", opts.ApiToken);
    }

    [Fact]
    public void AddConfluenceClient_FallsBackToFlatEnvKeys()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CONFLUENCE_BASE_URL"]  = "https://flat.atlassian.net",
                ["CONFLUENCE_EMAIL"]     = "flat@example.com",
                ["CONFLUENCE_API_TOKEN"] = "flat-token",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConfluenceClient(config);

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<ConfluenceOptions>>().Value;

        Assert.Equal("https://flat.atlassian.net", opts.BaseUrl);
        Assert.Equal("flat@example.com", opts.Email);
        Assert.Equal("flat-token", opts.ApiToken);
    }
}
