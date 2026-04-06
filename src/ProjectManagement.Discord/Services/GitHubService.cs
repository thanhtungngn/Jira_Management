using Discord;
using Microsoft.Extensions.Logging;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.GitHub.Models;
using ProjectManagement.Discord.Formatting;

namespace ProjectManagement.Discord.Services;

/// <summary>
/// Implements <see cref="IGitHubService"/> by delegating to <see cref="IGitHubClient"/>
/// and formatting results as Discord embeds.
/// </summary>
public sealed class GitHubService : IGitHubService
{
    private readonly IGitHubClient _client;
    private readonly ILogger<GitHubService> _logger;

    /// <summary>Initialises the service.</summary>
    public GitHubService(IGitHubClient client, ILogger<GitHubService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Embed> ListRepositoriesAsync()
    {
        _logger.LogInformation("[Discord/GitHub] list_repositories");
        try
        {
            var repos = await _client.ListRepositoriesAsync();
            return GitHubEmbedBuilder.BuildRepoList(repos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list GitHub repositories");
            return GitHubEmbedBuilder.BuildError("GitHub Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> GetRepositoryAsync(string owner, string repo)
    {
        _logger.LogInformation("[Discord/GitHub] get_repository: {Owner}/{Repo}", owner, repo);
        try
        {
            var repository = await _client.GetRepositoryAsync(owner, repo);
            return GitHubEmbedBuilder.BuildRepoDetail(repository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get repository {Owner}/{Repo}", owner, repo);
            return GitHubEmbedBuilder.BuildError("GitHub Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> ListIssuesAsync(string owner, string repo, string state)
    {
        _logger.LogInformation("[Discord/GitHub] list_issues: {Owner}/{Repo} state={State}", owner, repo, state);
        try
        {
            var issues = await _client.ListIssuesAsync(owner, repo, state);
            return GitHubEmbedBuilder.BuildIssueList(issues, owner, repo, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list issues for {Owner}/{Repo}", owner, repo);
            return GitHubEmbedBuilder.BuildError("GitHub Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> GetIssueAsync(string owner, string repo, int issueNumber)
    {
        _logger.LogInformation("[Discord/GitHub] get_issue: {Owner}/{Repo}#{Number}", owner, repo, issueNumber);
        try
        {
            var issue = await _client.GetIssueAsync(owner, repo, issueNumber);
            return GitHubEmbedBuilder.BuildIssueDetail(issue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get issue {Owner}/{Repo}#{Number}", owner, repo, issueNumber);
            return GitHubEmbedBuilder.BuildError("GitHub Error", ex.Message);
        }
    }
}
