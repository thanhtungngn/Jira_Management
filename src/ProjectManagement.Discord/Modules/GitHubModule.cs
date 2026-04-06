using System.Diagnostics.CodeAnalysis;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectManagement.Discord.Services;

namespace ProjectManagement.Discord.Modules;

/// <summary>
/// Slash-command module that exposes GitHub operations under the <c>/github</c> group.
/// Each command delegates business logic to <see cref="IGitHubService"/>.
/// </summary>
/// <remarks>
/// This class is a thin Discord.Net integration wrapper.
/// All testable business logic lives in <see cref="IGitHubService"/> and its implementation.
/// </remarks>
[ExcludeFromCodeCoverage]
[Group("github", "GitHub repository commands")]
public sealed class GitHubModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IGitHubService _service;
    private readonly ILogger<GitHubModule> _logger;

    /// <summary>Initialises the module.</summary>
    public GitHubModule(IGitHubService service, ILogger<GitHubModule> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>List repositories accessible to the configured GitHub token.</summary>
    [SlashCommand("repos", "List your GitHub repositories")]
    public async Task ReposAsync()
    {
        await DeferAsync();
        _logger.LogDebug("GitHub repos");
        var embed = await _service.ListRepositoriesAsync();
        await FollowupAsync(embed: embed);
    }

    /// <summary>Get details for a specific repository.</summary>
    [SlashCommand("repo", "Get details of a GitHub repository")]
    public async Task RepoAsync(
        [Summary("owner", "Repository owner or organisation")] string owner,
        [Summary("name",  "Repository name")]                  string name)
    {
        await DeferAsync();
        _logger.LogDebug("GitHub repo: {Owner}/{Repo}", owner, name);
        var embed = await _service.GetRepositoryAsync(owner, name);
        await FollowupAsync(embed: embed);
    }

    /// <summary>List issues for a repository, filtered by state.</summary>
    [SlashCommand("issues", "List issues in a GitHub repository")]
    public async Task IssuesAsync(
        [Summary("owner", "Repository owner or organisation")] string owner,
        [Summary("repo",  "Repository name")]                  string repo,
        [Summary("state", "Issue state: open, closed, or all (default: open)")] string state = "open")
    {
        await DeferAsync();
        _logger.LogDebug("GitHub issues: {Owner}/{Repo} state={State}", owner, repo, state);
        var embed = await _service.ListIssuesAsync(owner, repo, state);
        await FollowupAsync(embed: embed);
    }

    /// <summary>Get details for a specific issue.</summary>
    [SlashCommand("issue", "Get details of a GitHub issue")]
    public async Task IssueAsync(
        [Summary("owner",  "Repository owner or organisation")] string owner,
        [Summary("repo",   "Repository name")]                  string repo,
        [Summary("number", "Issue number")]                     int    number)
    {
        await DeferAsync();
        _logger.LogDebug("GitHub issue: {Owner}/{Repo}#{Number}", owner, repo, number);
        var embed = await _service.GetIssueAsync(owner, repo, number);
        await FollowupAsync(embed: embed);
    }
}
