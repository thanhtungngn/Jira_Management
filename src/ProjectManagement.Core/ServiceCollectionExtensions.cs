using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Trello;

namespace ProjectManagement.Core;

/// <summary>Extension methods for registering all Project Management clients with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Jira HTTP client and binds <see cref="JiraOptions"/> from
    /// <c>configuration["Jira"]</c>. Falls back to the legacy flat keys
    /// <c>JIRA_BASE_URL</c>, <c>JIRA_EMAIL</c>, and <c>JIRA_API_TOKEN</c> when the
    /// section is absent.
    /// </summary>
    public static IServiceCollection AddJiraClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JiraOptions>(opts =>
        {
            // Prefer structured section; fall back to legacy flat env vars
            var section = configuration.GetSection(JiraOptions.SectionName);
            if (section.Exists())
            {
                section.Bind(opts);
            }
            else
            {
                opts.BaseUrl  = configuration["JIRA_BASE_URL"]  ?? string.Empty;
                opts.Email    = configuration["JIRA_EMAIL"]     ?? string.Empty;
                opts.ApiToken = configuration["JIRA_API_TOKEN"] ?? string.Empty;
            }
        });

        services.AddHttpClient<IJiraClient, JiraClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<JiraOptions>>().Value;

            if (string.IsNullOrWhiteSpace(opts.BaseUrl))
                throw new InvalidOperationException("Jira BaseUrl is required (Jira:BaseUrl or JIRA_BASE_URL).");
            if (string.IsNullOrWhiteSpace(opts.Email))
                throw new InvalidOperationException("Jira Email is required (Jira:Email or JIRA_EMAIL).");
            if (string.IsNullOrWhiteSpace(opts.ApiToken))
                throw new InvalidOperationException("Jira ApiToken is required (Jira:ApiToken or JIRA_API_TOKEN).");

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{opts.Email}:{opts.ApiToken}"));
            client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/rest/api/3/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }

    /// <summary>
    /// Registers the Trello HTTP client and binds <see cref="TrelloOptions"/> from
    /// <c>configuration["Trello"]</c>. Falls back to flat keys
    /// <c>TRELLO_API_KEY</c> and <c>TRELLO_TOKEN</c>.
    /// </summary>
    public static IServiceCollection AddTrelloClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TrelloOptions>(opts =>
        {
            var section = configuration.GetSection(TrelloOptions.SectionName);
            if (section.Exists())
            {
                section.Bind(opts);
            }
            else
            {
                opts.ApiKey = configuration["TRELLO_API_KEY"] ?? string.Empty;
                opts.Token  = configuration["TRELLO_TOKEN"]   ?? string.Empty;
            }
        });

        services.AddHttpClient<ITrelloClient, TrelloClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<TrelloOptions>>().Value;

            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                throw new InvalidOperationException("Trello ApiKey is required (Trello:ApiKey or TRELLO_API_KEY).");
            if (string.IsNullOrWhiteSpace(opts.Token))
                throw new InvalidOperationException("Trello Token is required (Trello:Token or TRELLO_TOKEN).");

            client.BaseAddress = new Uri("https://api.trello.com/1/");
            client.DefaultRequestHeaders.Add("Authorization",
                $"OAuth oauth_consumer_key=\"{opts.ApiKey}\", oauth_token=\"{opts.Token}\"");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }

    /// <summary>
    /// Registers the GitHub HTTP client and binds <see cref="GitHubOptions"/> from
    /// <c>configuration["GitHub"]</c>. Falls back to flat key <c>GITHUB_TOKEN</c>.
    /// </summary>
    public static IServiceCollection AddGitHubClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GitHubOptions>(opts =>
        {
            var section = configuration.GetSection(GitHubOptions.SectionName);
            if (section.Exists())
            {
                section.Bind(opts);
            }
            else
            {
                opts.Token = configuration["GITHUB_TOKEN"] ?? string.Empty;
            }
        });

        services.AddHttpClient<IGitHubClient, GitHubClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<GitHubOptions>>().Value;

            if (string.IsNullOrWhiteSpace(opts.Token))
                throw new InvalidOperationException("GitHub Token is required (GitHub:Token or GITHUB_TOKEN).");

            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.Token);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
        });

        return services;
    }
}
