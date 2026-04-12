using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.GitHub.Models;

namespace ProjectManagement.Api.Controllers;

/// <summary>Endpoints for browsing GitHub repositories, branches, commits, and issues.</summary>
[ApiController]
[Route("api/repositories")]
[Route("api/github/repositories")]
[Produces("application/json")]
public class RepositoriesController : ControllerBase
{
    private readonly IGitHubClient _client;
    private readonly ILogger<RepositoriesController> _logger;

    public RepositoriesController(IGitHubClient client, ILogger<RepositoriesController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Returns repositories accessible by the authenticated user (up to 100).</summary>
    /// <returns>A list of repositories sorted by last update.</returns>
    /// <response code="200">Repositories retrieved successfully.</response>
    /// <response code="502">GitHub API returned an error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<GitHubRepository>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<GitHubRepository>>> GetRepositories()
    {
        _logger.LogInformation("Listing GitHub repositories");
        var repos = await _client.ListRepositoriesAsync();
        return Ok(repos);
    }

    /// <summary>Returns details of a specific GitHub repository.</summary>
    /// <param name="owner">The repository owner (user or organization).</param>
    /// <param name="repo">The repository name.</param>
    /// <returns>Repository details.</returns>
    /// <response code="200">Repository found.</response>
    /// <response code="404">Repository not found.</response>
    /// <response code="502">GitHub API returned an error.</response>
    [HttpGet("{owner}/{repo}")]
    [ProducesResponseType(typeof(GitHubRepository), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GitHubRepository>> GetRepository(string owner, string repo)
    {
        _logger.LogInformation("Getting repository {Owner}/{Repo}", owner, repo);
        var repository = await _client.GetRepositoryAsync(owner, repo);
        return Ok(repository);
    }

    /// <summary>Returns all branches in a GitHub repository.</summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <returns>A list of branches.</returns>
    /// <response code="200">Branches retrieved successfully.</response>
    /// <response code="502">GitHub API returned an error.</response>
    [HttpGet("{owner}/{repo}/branches")]
    [ProducesResponseType(typeof(List<GitHubBranch>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<GitHubBranch>>> GetBranches(string owner, string repo)
    {
        _logger.LogInformation("Listing branches for {Owner}/{Repo}", owner, repo);
        var branches = await _client.ListBranchesAsync(owner, repo);
        return Ok(branches);
    }

    /// <summary>Returns recent commits in a GitHub repository.</summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="branch">Optional branch name to filter by.</param>
    /// <param name="perPage">Number of commits per page (default 30, max 100).</param>
    /// <param name="page">Page number (default 1).</param>
    /// <returns>A list of commits.</returns>
    /// <response code="200">Commits retrieved successfully.</response>
    /// <response code="502">GitHub API returned an error.</response>
    [HttpGet("{owner}/{repo}/commits")]
    [ProducesResponseType(typeof(List<GitHubCommit>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<GitHubCommit>>> GetCommits(
        string owner,
        string repo,
        [FromQuery] string? branch = null,
        [FromQuery] int perPage = 30,
        [FromQuery] int page = 1)
    {
        _logger.LogInformation("Listing commits for {Owner}/{Repo}", owner, repo);
        var commits = await _client.ListCommitsAsync(new ListCommitsRequest
        {
            Owner   = owner,
            Repo    = repo,
            Branch  = branch,
            PerPage = perPage,
            Page    = page,
        });
        return Ok(commits);
    }

    /// <summary>Returns issues in a GitHub repository.</summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="state">Issue state filter: <c>open</c>, <c>closed</c>, or <c>all</c> (default: <c>open</c>).</param>
    /// <returns>A list of issues.</returns>
    /// <response code="200">Issues retrieved successfully.</response>
    /// <response code="502">GitHub API returned an error.</response>
    [HttpGet("{owner}/{repo}/issues")]
    [ProducesResponseType(typeof(List<GitHubIssue>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<GitHubIssue>>> GetIssues(
        string owner,
        string repo,
        [FromQuery] string state = "open")
    {
        _logger.LogInformation("Listing issues for {Owner}/{Repo}", owner, repo);
        var issues = await _client.ListIssuesAsync(owner, repo, state);
        return Ok(issues);
    }

    /// <summary>Returns details of a specific GitHub issue.</summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="issueNumber">The issue number.</param>
    /// <returns>The issue details.</returns>
    /// <response code="200">Issue found.</response>
    /// <response code="404">Issue not found.</response>
    /// <response code="502">GitHub API returned an error.</response>
    [HttpGet("{owner}/{repo}/issues/{issueNumber:int}")]
    [ProducesResponseType(typeof(GitHubIssue), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GitHubIssue>> GetIssue(string owner, string repo, int issueNumber)
    {
        _logger.LogInformation("Getting issue #{IssueNumber} for {Owner}/{Repo}", issueNumber, owner, repo);
        var issue = await _client.GetIssueAsync(owner, repo, issueNumber);
        return Ok(issue);
    }

    /// <summary>Creates a new issue in a GitHub repository.</summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="request">Issue details. <c>title</c> is required.</param>
    /// <returns>The newly created issue.</returns>
    /// <response code="201">Issue created successfully.</response>
    /// <response code="400">Request body is missing or invalid.</response>
    /// <response code="502">GitHub API returned an error.</response>
    [HttpPost("{owner}/{repo}/issues")]
    [ProducesResponseType(typeof(GitHubIssue), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GitHubIssue>> CreateIssue(
        string owner,
        string repo,
        [FromBody] CreateIssueRequest request)
    {
        _logger.LogInformation("Creating issue '{Title}' in {Owner}/{Repo}", request.Title, owner, repo);
        var issue = await _client.CreateIssueAsync(owner, repo, request);
        return CreatedAtAction(nameof(GetIssue), new { owner, repo, issueNumber = issue.Number }, issue);
    }
}
