using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectManagement.Core.GitHub.Models;

namespace ProjectManagement.Core.GitHub;

public class GitHubClient : IGitHubClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public GitHubClient(HttpClient httpClient, ILogger<GitHubClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<GitHubClient>.Instance;
    }

    public async Task<List<GitHubRepository>> ListRepositoriesAsync()
    {
        _logger.LogDebug("Listing GitHub repositories");
        var response = await _httpClient.GetAsync("user/repos?per_page=100&sort=updated");
        await EnsureSuccessAsync(response);
        var repos = await response.Content.ReadFromJsonAsync<List<GitHubRepository>>(JsonOptions) ?? [];
        _logger.LogInformation("Retrieved {Count} repositories", repos.Count);
        return repos;
    }

    public async Task<GitHubRepository> GetRepositoryAsync(string owner, string repo)
    {
        _logger.LogDebug("Fetching repository {Owner}/{Repo}", owner, repo);
        var response = await _httpClient.GetAsync(
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}");
        await EnsureSuccessAsync(response);
        var repository = await response.Content.ReadFromJsonAsync<GitHubRepository>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from GitHub.");
        _logger.LogInformation("Retrieved repository {FullName}", repository.FullName);
        return repository;
    }

    public async Task<List<GitHubBranch>> ListBranchesAsync(string owner, string repo)
    {
        _logger.LogDebug("Listing branches for {Owner}/{Repo}", owner, repo);
        var response = await _httpClient.GetAsync(
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/branches");
        await EnsureSuccessAsync(response);
        var branches = await response.Content.ReadFromJsonAsync<List<GitHubBranch>>(JsonOptions) ?? [];
        _logger.LogInformation("Retrieved {Count} branches for {Owner}/{Repo}", branches.Count, owner, repo);
        return branches;
    }

    public async Task<List<GitHubCommit>> ListCommitsAsync(ListCommitsRequest request)
    {
        _logger.LogDebug("Listing commits for {Owner}/{Repo}", request.Owner, request.Repo);
        var url = $"repos/{Uri.EscapeDataString(request.Owner)}/{Uri.EscapeDataString(request.Repo)}/commits" +
                  $"?per_page={request.PerPage}&page={request.Page}";
        if (!string.IsNullOrWhiteSpace(request.Branch))
            url += $"&sha={Uri.EscapeDataString(request.Branch)}";

        var response = await _httpClient.GetAsync(url);
        await EnsureSuccessAsync(response);
        var commits = await response.Content.ReadFromJsonAsync<List<GitHubCommit>>(JsonOptions) ?? [];
        _logger.LogInformation("Retrieved {Count} commits for {Owner}/{Repo}", commits.Count, request.Owner, request.Repo);
        return commits;
    }

    public async Task<List<GitHubIssue>> ListIssuesAsync(string owner, string repo, string state = "open")
    {
        _logger.LogDebug("Listing issues for {Owner}/{Repo}", owner, repo);
        var response = await _httpClient.GetAsync(
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues?state={state}&per_page=100");
        await EnsureSuccessAsync(response);
        var issues = await response.Content.ReadFromJsonAsync<List<GitHubIssue>>(JsonOptions) ?? [];
        _logger.LogInformation("Retrieved {Count} issues for {Owner}/{Repo}", issues.Count, owner, repo);
        return issues;
    }

    public async Task<GitHubIssue> GetIssueAsync(string owner, string repo, int issueNumber)
    {
        _logger.LogDebug("Fetching issue #{IssueNumber} for {Owner}/{Repo}", issueNumber, owner, repo);
        var response = await _httpClient.GetAsync(
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues/{issueNumber}");
        await EnsureSuccessAsync(response);
        var issue = await response.Content.ReadFromJsonAsync<GitHubIssue>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from GitHub.");
        _logger.LogInformation("Retrieved issue #{IssueNumber}", issue.Number);
        return issue;
    }

    public async Task<GitHubIssue> CreateIssueAsync(string owner, string repo, CreateIssueRequest request)
    {
        _logger.LogDebug("Creating issue '{Title}' in {Owner}/{Repo}", request.Title, owner, repo);
        var body = new Dictionary<string, object> { ["title"] = request.Title };
        if (request.Body is not null) body["body"] = request.Body;
        if (request.Labels is not null) body["labels"] = request.Labels;
        if (request.Assignees is not null) body["assignees"] = request.Assignees;

        var response = await _httpClient.PostAsJsonAsync(
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues", body, JsonOptions);
        await EnsureSuccessAsync(response);
        var issue = await response.Content.ReadFromJsonAsync<GitHubIssue>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from GitHub.");
        _logger.LogInformation("Created issue #{IssueNumber} in {Owner}/{Repo}", issue.Number, owner, repo);
        return issue;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"GitHub API error {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
        }
    }
}
