using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.GitHub.Models;

namespace ProjectManagement.Mcp.GitHub;

[McpServerToolType]
public sealed class GitHubTools
{
    private readonly IGitHubClient _client;
    private readonly ILogger<GitHubTools> _logger;

    public GitHubTools(IGitHubClient client, ILogger<GitHubTools> logger)
    {
        _client = client;
        _logger = logger;
    }

    [McpServerTool(Name = "list_repositories"), Description("List GitHub repositories accessible by the authenticated user.")]
    public async Task<List<GitHubRepository>> ListRepositoriesAsync()
    {
        _logger.LogInformation("[MCP] list_repositories");
        return await _client.ListRepositoriesAsync();
    }

    [McpServerTool(Name = "get_repository"), Description("Get details of a specific GitHub repository.")]
    public async Task<GitHubRepository> GetRepositoryAsync(string owner, string repo)
    {
        _logger.LogInformation("[MCP] get_repository: {Owner}/{Repo}", owner, repo);
        return await _client.GetRepositoryAsync(owner, repo);
    }

    [McpServerTool(Name = "list_branches"), Description("List branches of a GitHub repository.")]
    public async Task<List<GitHubBranch>> ListBranchesAsync(string owner, string repo)
    {
        _logger.LogInformation("[MCP] list_branches: {Owner}/{Repo}", owner, repo);
        return await _client.ListBranchesAsync(owner, repo);
    }

    [McpServerTool(Name = "list_commits"), Description("List commits in a GitHub repository, optionally filtered by branch.")]
    public async Task<List<GitHubCommit>> ListCommitsAsync(
        string owner,
        string repo,
        string? branch = null,
        int perPage = 30,
        int page = 1)
    {
        _logger.LogInformation("[MCP] list_commits: {Owner}/{Repo} branch={Branch}", owner, repo, branch);
        return await _client.ListCommitsAsync(new ListCommitsRequest
        {
            Owner   = owner,
            Repo    = repo,
            Branch  = branch,
            PerPage = perPage,
            Page    = page,
        });
    }

    [McpServerTool(Name = "list_issues"), Description("List issues in a GitHub repository filtered by state (open/closed/all).")]
    public async Task<List<GitHubIssue>> ListIssuesAsync(string owner, string repo, string state = "open")
    {
        _logger.LogInformation("[MCP] list_issues: {Owner}/{Repo} state={State}", owner, repo, state);
        return await _client.ListIssuesAsync(owner, repo, state);
    }

    [McpServerTool(Name = "get_github_issue"), Description("Get details of a specific GitHub issue by its number.")]
    public async Task<GitHubIssue> GetIssueAsync(string owner, string repo, int issueNumber)
    {
        _logger.LogInformation("[MCP] get_github_issue: {Owner}/{Repo}#{IssueNumber}", owner, repo, issueNumber);
        return await _client.GetIssueAsync(owner, repo, issueNumber);
    }

    [McpServerTool(Name = "create_github_issue"), Description("Create a new issue in a GitHub repository.")]
    public async Task<GitHubIssue> CreateIssueAsync(
        string owner,
        string repo,
        string title,
        string? body = null)
    {
        _logger.LogInformation("[MCP] create_github_issue: {Owner}/{Repo} title={Title}", owner, repo, title);
        return await _client.CreateIssueAsync(owner, repo, new CreateIssueRequest
        {
            Title = title,
            Body  = body,
        });
    }
}
